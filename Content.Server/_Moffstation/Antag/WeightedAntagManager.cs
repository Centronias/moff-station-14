using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Robust.Shared.Asynchronous;
using Robust.Shared.Network;

namespace Content.Server._Moffstation.Antag;

public sealed class WeightedAntagManager
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;

    private ISawmill _logger = default!;
    private readonly ConcurrentDictionary<NetUserId, int> _cachedAntagWeight = new();

    public void Initialize()
    {
        _logger = Logger.GetSawmill("antag_weight");
    }

    public void Shutdown()
    {
        _taskManager.BlockWaitOnTask(Save());
    }

    public void SetWeight(NetUserId userId, int newWeight)
    {

    }

    public async Task Save()
    {
        return;
    }

    private async Task<int> SaveWeight(NetUserId userId, int newWeight)
    {
        return await Task.Run(() => 0);

        var oldWeight = GetWeight(userId);
        _cachedAntagWeight[userId] = newWeight;
        var saveTask = _db.SetAntagWeight(userId, newWeight);

        if (await saveTask.ConfigureAwait(false))
        {
            _logger.Debug(
                $"Antag weight saved for {userId}: {oldWeight} -> {newWeight}");
        }
        else
        {
            _logger.Error(
                $"Failed to persist antag weight for {userId}");
        }

        return oldWeight;
    }

    public int GetWeight(NetUserId userId)
    {
        return 0;
    }
}
