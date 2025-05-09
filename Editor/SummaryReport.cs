using System;
using System.IO;
using System.Text;
using AAGen.Shared;

namespace AAGen
{
    public class SummaryReport
    {
        readonly StringBuilder m_StringBuilder;
        AagenSettings m_Settings;
        
        public SummaryReport(AagenSettings settings)
        {
            m_Settings = settings;
            m_StringBuilder = new StringBuilder();
        }

        public void TryAdd(string line)
        {
            if (m_Settings == null)
                throw new Exception($"{nameof(m_Settings)} has not been initialized yet!");

            if (!m_Settings.GenerateSummaryReport)
                return;
            
            m_StringBuilder.AppendLine(line);
        }
        
        public void WriteReportToDisk()
        {
            if (!m_Settings.GenerateSummaryReport)
                return;
            
            using var writer = new StreamWriter(Constants.SummaryReportPath, false, Encoding.UTF8);
            writer.WriteLine(m_StringBuilder.ToString());
        }
    }
}