using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SAM.Analytical.Revit.UI.Properties;
using SAM.Core.Revit.UI;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace SAM.Analytical.Revit.UI
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class RemoveParameters : PushButtonExternalCommand
    {
        public override string RibbonPanelName => "Shared Parameters";

        public override int Index => 6;

        public override BitmapSource BitmapSource => Core.Windows.Convert.ToBitmapSource(Resources.SAM_RemoveParameters, 32, 32);

        public override string Text => "Remove\nParameters";

        public override string ToolTip => "Remove Parameters";

        public override string AvailabilityClassName => null;

        public override void Execute()
        {
            Document document = ExternalCommandData?.Application?.ActiveUIDocument?.Document;
            if (document == null)
            {
                return;
            }

            string path_Excel = null;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Excel Workbook|*.xlsm;*.xlsx";
                openFileDialog.Title = "Select Excel file";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    path_Excel = openFileDialog.FileName;
                }
            }

            if (string.IsNullOrEmpty(path_Excel))
            {
                return;
            }

            object[,] objects = Core.Excel.Query.Values(path_Excel, "Live");
            if (objects == null || objects.GetLength(0) <= 1 || objects.GetLength(1) < 11)
            {
                return;
            }

            int index_Group = 2;
            int index_Name = 7;

            List<string> names_Selected = Query.ParameterNames(objects, index_Group, index_Name);
            if (names_Selected == null || names_Selected.Count == 0)
            {
                return;
            }

            using (Transaction transaction = new Transaction(document, "Remove Parameters"))
            {
                transaction.Start();

                BindingMap bindingMap = document.ParameterBindings;

                DefinitionBindingMapIterator definitionBindingMapIterator = bindingMap.ForwardIterator();

                List<Definition> definitions = new List<Definition>();

                while (definitionBindingMapIterator.MoveNext())
                {
                    Definition definition = definitionBindingMapIterator.Key;
                    //Autodesk.Revit.DB.Binding aBinding = aDefinitionBindingMapIterator.Current as Autodesk.Revit.DB.Binding;

                    definitions.Add(definition);
                }

                List<string> names = new List<string>();
                using (Core.Windows.Forms.ProgressForm progressForm = new Core.Windows.Forms.ProgressForm("Creating Shared Parameters", objects.GetLength(0)))
                {
                    for (int i = 5; i <= objects.GetLength(0); i++)
                    {
                        string name = objects[i, index_Name] as string;
                        if (!string.IsNullOrEmpty(name))
                        {
                            Definition definition = definitions.Find(x => x.Name == name);
                            if (definition != null)
                            {
                                bindingMap.Remove(definition);
                            }
                        }
                        else
                        {
                            name = "???";
                        }

                        progressForm.Update(name);
                    }
                }

                transaction.Commit();
            }
        }
    }
}
