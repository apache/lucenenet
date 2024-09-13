using Docfx.Common;
using Docfx.Plugins;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;

namespace LuceneDocsPlugins;

[Export(nameof(EnvironmentVariableProcessor), typeof(IPostProcessor))]
public class EnvironmentVariableProcessor : IPostProcessor
{
    public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
    {
        return metadata;
    }

    public Manifest Process(Manifest manifest, string outputFolder)
    {
        foreach (var manifestItem in manifest.Files.Where(x => x.Type == "Conceptual"))
        {
            foreach (var manifestItemOutputFile in manifestItem.Output)
            {
                var outputPath = Path.Combine(outputFolder, manifestItemOutputFile.Value.RelativePath);

                var content = File.ReadAllText(outputPath);

                Logger.LogInfo($"Replacing environment variables in {outputPath}");

                var newContent = EnvironmentVariableUtil.ReplaceEnvironmentVariables(content);

                if (content == newContent)
                {
                    continue;
                }

                Logger.LogInfo($"Writing new content to {outputPath}");

                File.WriteAllText(outputPath, newContent);
            }
        }

        return manifest;
    }
}
