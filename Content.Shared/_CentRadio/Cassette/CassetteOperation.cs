using Content.Shared._Moffstation.Extensions;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._CentRadio.Cassette;

[Serializable, NetSerializable]
public enum CassetteOperation
{
    Stop,
    Play,
    FastForward,
    Rewind,
}

public static class CassetteOperationExt
{
    extension(CassetteOperation op)
    {
        /// Returns how many tape-time-seconds per second are moved when a cassette is undergoing <paramref name="op"/>
        /// in a <see cref="CassetteContainerComponent"/> with <paramref name="fastOperationSpeed"/>.
        public FixedPoint2 GetOperationFactor(FixedPoint2? fastOperationSpeed) => op switch
        {
            CassetteOperation.Stop => 0,
            CassetteOperation.Play => 1,
            CassetteOperation.FastForward => fastOperationSpeed ?? 0,
            CassetteOperation.Rewind => -(fastOperationSpeed ?? 0),
            _ => op.UnknownEnumVariant<CassetteOperation, FixedPoint2>(),
        };

        public string ToLocParam() => op switch
        {
            CassetteOperation.Stop => "stop",
            CassetteOperation.Play => "play",
            CassetteOperation.FastForward => "fastforward",
            CassetteOperation.Rewind => "rewind",
            _ => op.UnknownEnumVariant<CassetteOperation, string>(),
        };
    }
}
