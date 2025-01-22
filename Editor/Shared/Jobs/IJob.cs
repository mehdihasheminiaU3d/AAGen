using System.Collections;

namespace AAGen.Shared
{
    public interface IJob
    {
        string Name { get; set; }
        IEnumerator Run();
        bool IsComplete { get; }
    }
}
