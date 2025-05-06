using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.DeviceNetwork.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Cargo.Components;

[RegisterComponent]
public sealed partial class AutomaticCargoClientComponent : Component;

public sealed partial class AutomaticCargoClientSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly DeviceListSystem _deviceList = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AutomaticCargoClientComponent, ComponentInit>(OnComponentInit);
    }

    public void OnComponentInit(Entity<AutomaticCargoClientComponent> entity, ref ComponentInit args)
    {
        Predicate<EntityUid> isCargoServerOnStation;

        // Special case for handling the consoles on the ATS. From the console, if it's on a trade station, figure out
        // the station that trade station's attached to. Then, any cargo server on the station will be auto-linked to
        // the ATS console.
        if (_station.GetOwningStation(entity) is { } tradeStation &&
            HasComp<TradeStationComponent>(tradeStation) &&
            _station.GetOwningStation(tradeStation) is { } mainStation)
        {
            isCargoServerOnStation = cargoServer => _station.GetOwningStation(cargoServer) == mainStation;
        }
        else
        {
            // Otherwise, only link to cargo servers that are on the same grid this device is on.
            var clientTransform = Transform(entity);
            isCargoServerOnStation = cargoServer => Transform(cargoServer).GridUid == clientTransform.GridUid;
        }

        List<EntityUid> candidates = [];
        var query = EntityQueryEnumerator<StationBankAccountComponent>();
        while (query.MoveNext(out var cargoServer, out _))
        {
            if (isCargoServerOnStation(cargoServer))
                candidates.Add(cargoServer);
        }

        if (candidates.Count == 1)
            _deviceList.UpdateDeviceList(candidates.Single(), [entity], merge: true);
    }
}
