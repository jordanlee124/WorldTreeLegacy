using UnityEditor;

namespace Worldtree.Editor
{
    /// <summary>Enforces the model import settings from docs/BlenderPipeline.md on everything under Art/Models.</summary>
    public sealed class ModelImportConventions : AssetPostprocessor
    {
        const string Folder = "Assets/_Game/Art/Models/";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;                      // Blender already exports in metres
            importer.importNormals = ModelImporterNormals.Import; // normals are authored in Blender
            importer.importCameras = false;
            importer.importLights = false;
            var name = System.IO.Path.GetFileName(assetPath);
            importer.animationType = name.StartsWith("SK_") ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None;
        }
    }
}
