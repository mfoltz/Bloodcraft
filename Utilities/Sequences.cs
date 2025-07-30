using ProjectM;
using Unity.Entities;

namespace Bloodcraft.Utilities;
internal static class Sequences
{
    public struct SequenceRequest
    {
        public string SequenceName;
        public SequenceGUID SequenceGuid;
        public Entity Target;
        public Entity Secondary;
        public float Scale;
    }

    static readonly Queue<SequenceRequest> _sequenceQueue = new();
    static void Enqueue(SequenceRequest sequenceRequest) => _sequenceQueue.Enqueue(sequenceRequest);
    public static bool TryDequeue(out SequenceRequest sequenceRequest) => _sequenceQueue.TryDequeue(out sequenceRequest);
    public static void PlaySequence(this Entity primary, SequenceGUID sequenceGuid, Entity secondary = default)
    {
        Enqueue(new SequenceRequest
        {
            SequenceName = sequenceGuid.GetSequenceName(),
            Target = primary,
            SequenceGuid = sequenceGuid,
            Scale = 1f,
            Secondary = secondary
        });
    }
}
