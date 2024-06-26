using System.IO;
using System.Reflection;
using Serilog;
using Specify;
using Specify.Configuration;
using TestStack.BDDfy.Configuration;
using TinyIoC;

namespace prototype_server.Specs.Config
{
    /// <summary>
    /// The startup class to configure Specify with the default TinyIoc container. 
    /// Make any changes to the default configuration settings in this file.
    /// </summary>
    public class SpecifyBootstrapper : DefaultBootstrapper
    {
        public SpecifyBootstrapper()
        {
            Configurator.Scanners.DefaultMethodNameStepScanner.Disable();
            Configurator.Scanners.Add(() => new ContextSpecification());
            
            LoggingEnabled = false;

            var reportsDir = Path.GetDirectoryName(GetType().GetTypeInfo().Assembly.Location) + "/Reports";
            
            Directory.CreateDirectory(reportsDir);
            
            HtmlReport.ReportHeader = "Prototype Game Server";
            HtmlReport.ReportDescription = "Feature Specs";
            HtmlReport.ReportType = HtmlReportConfiguration.HtmlReportType.Classic;
            HtmlReport.OutputPath = reportsDir;
            HtmlReport.OutputFileName = "specs-report.html";
            
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.LiterateConsole()
                .WriteTo.RollingFile("log-{Date}.txt")
                .CreateLogger();
        }
        
        /// <summary>
        /// Register any additional items into the TinyIoc container or leave it as it is. 
        /// </summary>
        /// <param name="container">The <see cref="TinyIoCContainer"/> container.</param>
        public override void ConfigureContainer(TinyIoCContainer container)
        {
        }
    }
    
    public interface IPerScenarioActions
    {
        void Before(IContainer container);
        void After();
    }
    
    public class ResetDatabaseAction : IPerScenarioActions
    {
        public void Before(IContainer container)
        {
            
        }

        public void After()
        {
        }
    }
}