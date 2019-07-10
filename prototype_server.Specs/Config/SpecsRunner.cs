using System;
using System.Linq;
using NSpec;
using NSpec.Domain;
using NSpec.Domain.Formatters;

namespace Specs.Config 
{
    internal class SpecsRunner 
    {
        internal static void Main(string[] args) {
            var tagOrClassName = string.Join(",", args);
            var types = typeof(SpecsRunner).Assembly.GetTypes();
            var finder = new SpecFinder(types, string.Empty);
            var tagsFilter = new Tags().Parse(tagOrClassName);
            var builder = new ContextBuilder(finder, tagsFilter, new DefaultConventions());
            var runner = new ContextRunner(tagsFilter, new ConsoleFormatter(), false);
            
            var results = runner.Run(builder.Contexts().Build());
            
            Environment.Exit(results.Failures().Count());
        }
    }
}