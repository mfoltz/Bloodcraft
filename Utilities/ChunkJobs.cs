using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Utilities;
public static class ChunkJobs
{
    public interface IJobChunk
    {
        bool IsComplete { get; }
        void Execute(ref ArchetypeChunk chunk);
    }

    public static void ForEach<T>(this EntityQuery query, ref T job) where T : struct, IJobChunk
    {
        var chunks = query.ToArchetypeChunkArray(Allocator.Temp);

        try
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                var chunk = chunks[i];
                job.Execute(ref chunk);

                if (job.IsComplete)
                    break;
            }
        }
        finally
        {
            if (chunks.IsCreated)
                chunks.Dispose();
        }
    }
}
