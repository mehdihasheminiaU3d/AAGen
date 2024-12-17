using System.Collections;

namespace AAGen.Runtime
{
    public interface IJob
    {
        string Name { get; set; }
        IEnumerator Run();
        bool IsComplete { get; }
    }
}
