namespace Bloodcraft.Patches.UpdateBuffsBufferDestroyPatchNS;

interface IBuffDestroyHandler
{
    bool CanHandle(UpdateBuffDestroyContext ctx);
    bool Handle(UpdateBuffDestroyContext ctx);
}

