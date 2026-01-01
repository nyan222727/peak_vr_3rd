using UnityEditor;


namespace AmazingAssets.AdvancedDissolveEditor
{
    internal class DefaultLitShaderGUI : ShaderGUI
    {
        public override void OnGUI(UnityEditor.MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            //AmazingAssets
            AmazingAssets.AdvancedDissolveEditor.MaterialEditor.Init(properties);

            //Curved World
            AmazingAssets.AdvancedDissolveEditor.MaterialEditor.DrawCurvedWorldHeader(materialEditor, null);


            base.OnGUI(materialEditor, properties);
           

            //AmazingAssets
            AmazingAssets.AdvancedDissolveEditor.MaterialEditor.DrawDissolveOptions(materialEditor, false, false, true, true, false);
        }
    }
}