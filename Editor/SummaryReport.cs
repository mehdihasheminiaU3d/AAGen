using System.IO;
using System.Text;
using AAGen.Shared;

namespace AAGen
{
    public class SummaryReport
    {
        readonly StringBuilder m_StringBuilder = new StringBuilder();
        
        public void AppendLine(string line)
        {
            m_StringBuilder.AppendLine(line);
        }
        
        public void WriteReportToDisk()
        {
            using var writer = new StreamWriter(Constants.SummaryReportPath, false, Encoding.UTF8);
            writer.WriteLine(m_StringBuilder.ToString());
        }
    }
}