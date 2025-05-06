using System.Linq;
using Content.Server._Moffstation.Cargo.Events;
using Content.Server.Cargo.Components;
using Content.Server.Containers;
using Content.Server.Station.Components;
using Content.Shared.Cargo.Components;
using Content.Shared.Construction;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;

// Not Content.Server._Moffstation... because this is partial code for an existing system.
namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    private void InitializeServer()
    {
        SubscribeLocalEvent<StationCargoOrderDatabaseComponent, CargoApprovedOrderMessage>(OnApprovedOrderMessage);
        SubscribeLocalEvent<StationBankAccountComponent, DamageChangedEvent>(OnDamageChanged);

        SubscribeLocalEvent<StationBankAccountComponent, DeviceListUpdateEvent>(OnDeviceListUpdate);

        // Intercept the event before anyone can do anything with it!
        SubscribeLocalEvent<StationBankAccountComponent, MachineDeconstructedEvent>(
            OnMachineDeconstructed,
            before: [typeof(EmptyOnMachineDeconstructSystem), typeof(ItemSlotsSystem)]
        );
    }

    #region ServerEventHandlers

    private void OnDeviceListUpdate(Entity<StationBankAccountComponent> entity, ref DeviceListUpdateEvent args)
    {
        var previouslyConnected = args.Devices.ToHashSet();
        previouslyConnected.IntersectWith(args.OldDevices);

        // Add host references to devices added to the list.
        var newHostEvent = new CargoHostSetEvent(entity);
        foreach (var device in args.Devices)
        {
            if (!previouslyConnected.Contains(device))
                RaiseLocalEvent(device, newHostEvent);
        }

        // Remove host references from devices removed from the list.
        var removeHostEvent = new CargoHostSetEvent(null);
        foreach (var device in args.OldDevices)
        {
            if (!previouslyConnected.Contains(device))
                RaiseLocalEvent(device, removeHostEvent);
        }
    }

    private void OnApprovedOrderMessage(
        Entity<StationCargoOrderDatabaseComponent> entity,
        ref CargoApprovedOrderMessage args
    )
    {
        StationBankAccountComponent? bank = null;
        if (!Resolve(entity.Owner, ref bank))
            return;

        // Find our order again. It might have been dispatched or approved already
        var orderId = args.OrderId;
        var order = entity.Comp.Orders[args.Account]
            .Find(order => orderId == order.OrderId && !order.Approved);
        if (order == null)
        {
            return;
        }

        // Invalid order
        if (!_protoMan.HasIndex<EntityPrototype>(order.ProductId))
        {
            args.DenialReason = Loc.GetString("cargo-console-invalid-product");
            return;
        }

        var amount = GetOutstandingOrderCount(entity.Comp, args.Account);

        // Too many orders, avoid them getting spammed in the UI.
        if (amount >= entity.Comp.Capacity)
        {
            args.DenialReason = Loc.GetString("cargo-console-too-many");
            return;
        }

        // Cap orders so someone can't spam thousands.
        var cappedAmount = Math.Min(entity.Comp.Capacity - amount, order.OrderQuantity);

        if (cappedAmount != order.OrderQuantity)
        {
            order.OrderQuantity = cappedAmount;
            args.DenialReason = Loc.GetString("cargo-console-snip-snip");
        }

        var cost = order.Price * order.OrderQuantity;
        var accountBalance = GetBalanceFromAccount((entity, bank), args.Account);

        // Not enough balance
        if (cost > accountBalance)
        {
            args.DenialReason = Loc.GetString("cargo-console-insufficient-funds", ("cost", cost));
            return;
        }

        var ev = new FulfillCargoOrderEvent(order, entity);
        RaiseLocalEvent(ref ev);

        if (!ev.Handled)
        {
            if (GetCargoServerStation(entity) is { } station)
            {
                ev.FulfillmentEntity = TryFulfillOrder(station, order, entity.Comp);
            }

            if (ev.FulfillmentEntity == null)
            {
                args.DenialReason = Loc.GetString("cargo-console-unfulfilled");
                return;
            }
        }

        order.Approved = true;

        if (args.ShouldAnnounceFulfillment)
        {
            var tryGetIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(args.Approver, args.ApprovedOnDevice);
            RaiseLocalEvent(tryGetIdentityShortInfoEvent);
            order.SetApproverData(tryGetIdentityShortInfoEvent.Title);

            var message = Loc.GetString("cargo-console-unlock-approved-order-broadcast",
                ("productName", Loc.GetString(order.ProductName)),
                ("orderAmount", order.OrderQuantity),
                ("approver", order.Approver ?? string.Empty),
                ("cost", cost));
            _radio.SendRadioMessage(entity, message, args.AnnouncementChannel, entity, escapeMarkup: false);
            if (CargoOrderConsoleComponent.BaseAnnouncementChannel != args.AnnouncementChannel)
            {
                _radio.SendRadioMessage(entity,
                    message,
                    CargoOrderConsoleComponent.BaseAnnouncementChannel,
                    entity,
                    escapeMarkup: false);
            }
        }

        ConsolePopup(args.Approver,
            Loc.GetString("cargo-console-trade-station",
                ("destination", MetaData(ev.FulfillmentEntity!.Value).EntityName)));

        // Log order approval
        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.Approver):user} approved order [orderId:{order.OrderId}, quantity:{order.OrderQuantity}, product:{order.ProductId}, requester:{order.Requester}, reason:{order.Reason}] on account {args.Account} with balance at {accountBalance}");

        entity.Comp.Orders[args.Account].Remove(order);
        UpdateBankAccount((entity, bank), -cost, args.Account);
        UpdateOrders();
    }

    private void OnDamageChanged(Entity<StationBankAccountComponent> entity, ref DamageChangedEvent args)
    {
        // Eject 5% of money on being damaged.
        TryDropAndThrowMoney(entity, 0.05f);
    }

    private void OnMachineDeconstructed(Entity<StationBankAccountComponent> entity, ref MachineDeconstructedEvent args)
    {
        TryDropAndThrowMoney(entity);
    }

    #endregion

    #region ServerAccessors

    private Entity<StationBankAccountComponent, StationCargoOrderDatabaseComponent>? GetCargoHost(
        EntityUid client
    )
    {
        if (!TryComp<DeviceNetworkComponent>(client, out var deviceNetwork) ||
            deviceNetwork.DeviceLists.SingleOrDefault() is not { Valid: true } server ||
            !TryComp<StationBankAccountComponent>(server, out var bank) ||
            !TryComp<StationCargoOrderDatabaseComponent>(server, out var orders))
            return null;

        return (server, bank, orders);
    }

    private Entity<StationDataComponent>? GetCargoServerStation(EntityUid server)
    {
        if (_station.GetOwningStation(server) is { } stationEnt &&
            TryComp<StationDataComponent>(stationEnt, out var stationData))
        {
            return (stationEnt, stationData);
        }

        return null;
    }

    #endregion

    private HashSet<EntityUid> GetCargoClients(EntityUid cargoHost)
    {
        if (!TryComp<DeviceLinkSourceComponent>(cargoHost, out var source) ||
            !source.Outputs.TryGetValue("", out var clients))
            return [];

        return clients;
    }

    private void TryDropAndThrowMoney(Entity<StationBankAccountComponent> entity, float proportionToEject = 1.0f)
    {
        var total = 0;
        foreach (var (account, startingBalance) in entity.Comp.Accounts)
        {
            var amount = (int)MathF.Floor(startingBalance * proportionToEject);
            entity.Comp.Accounts[account] -= amount;
            total += amount;
        }

        if (total == 0)
            return;

        var cashProto = _protoMan.Index(entity.Comp.CashType);
        var coords = Transform(entity).Coordinates;
        var stacks = Math.Min(total / 1000, 20);
        var cashPerStack = total / stacks;
        foreach (var _ in Enumerable.Range(0, stacks))
        {
            var cash = _stack.Spawn(cashPerStack, cashProto, coords);
            _throwing.TryThrow(cash, _random.NextVector2(), baseThrowSpeed: 5f);
        }

        var ev = new BankBalanceUpdatedEvent(entity, entity.Comp.Accounts);
        RaiseLocalEvent(entity, ref ev, true);

        Dirty(entity);
    }
}
