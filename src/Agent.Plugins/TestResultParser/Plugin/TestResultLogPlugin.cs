using System;
using System.Linq;
using System.Threading.Tasks;
using Agent.Plugins.Log.TestResultParser.Contracts;
using Agent.Plugins.Log.TestResultParser.Plugin;
using Agent.Plugins.TestResultParser.Plugin;
using Agent.Sdk;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;

namespace Agent.Plugins.Log
{
    public class TestResultLogPlugin : IAgentLogPlugin
    {
        public string FriendlyName => "Test Result Log Parser";

        public async Task<bool> InitializeAsync(IAgentLogPluginContext context)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (CheckForPluginDisable(context))
                    {
                        return false; // disable the plugin
                    }


                    PopulatePipelineConfig(context);

                    _clientFactory = new ClientFactory(context.VssConnection);
                    _inputDataParser = new LogParserGateway();
                    _logger = new TraceLogger(context);

                    _inputDataParser.Initialize(_clientFactory, _pipelineConfig, _logger);
                }
                catch (Exception ex)
                {

                    _logger.Warning($"Unable to initialize {FriendlyName}");
                    context.Trace(ex.StackTrace);
                    return false;
                }

                return true;
            });
        }

        public async Task ProcessLineAsync(IAgentLogPluginContext context, Pipelines.TaskStepDefinitionReference step, string line)
        {
            await _inputDataParser.ProcessDataAsync(line);
        }

        public async Task FinalizeAsync(IAgentLogPluginContext context)
        {
            await Task.Run(() =>
            {
                _inputDataParser.Complete();
            });
        }

        private bool CheckForPluginDisable(IAgentLogPluginContext context)
        {
            return context.Steps == null || context.Steps.Any(x => x.Id.Equals(new Guid("0B0F01ED-7DDE-43FF-9CBB-E48954DAF9B1")));
            // check for PTR task or some other tasks to enable/disable
        }

        private void PopulatePipelineConfig(IAgentLogPluginContext context)
        {
            if (context.Variables.TryGetValue("system.teamProjectId", out var projectGuid))
            {
                _pipelineConfig.Project = new Guid(projectGuid.Value);
            }

            if (context.Variables.TryGetValue("build.buildId", out var buildId))
            {
                _pipelineConfig.BuildId = int.Parse(buildId.Value);
            }
        }

        private ILogParserGateway _inputDataParser;
        private IClientFactory _clientFactory;
        private ITraceLogger _logger;
        private readonly IPipelineConfig _pipelineConfig = new PipelineConfig();
    }
}
