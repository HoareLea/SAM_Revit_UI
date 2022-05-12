
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.Revit.UI.Forms
{
    public partial class CopyTagsForm : System.Windows.Forms.Form
    {
        private UIDocument uIDocument;

        public CopyTagsForm(UIDocument uIDocument)
        {
            this.uIDocument = uIDocument;

            InitializeComponent();
        }

        private void CopyTagsForm_Load(object sender, EventArgs e)
        {
            Document document = uIDocument?.Document;

            if(document == null)
            {
                return;
            }

            List<Autodesk.Revit.DB.View> views = new FilteredElementCollector(document).OfClass(typeof(Autodesk.Revit.DB.View)).Cast<Autodesk.Revit.DB.View>().ToList();
            views.RemoveAll(x => x == null || !x.IsTemplate || x.ViewType != ViewType.FloorPlan);
            if(views == null || views.Count == 0)
            {
                return;
            }

            ComboBoxControl_SourceTemplate.SelectedIndexChanged += new EventHandler(ComboBoxControl_SourceTemplate_SelectedIndexChanged);
            ComboBoxControl_SourceTagType.SelectedIndexChanged += new EventHandler(ComboBoxControl_SourceTagType_SelectedIndexChanged);

            ComboBoxControl_SourceTemplate.AddRange(views, view => view.Name);
            if(!ComboBoxControl_SourceTemplate.SetSelectedItem(UIDocument.ActiveView))
            {
                ComboBoxControl_SourceTemplate.SetSelectedItem(views[0]);
            }

            ComboBoxControl_DestinationTemplate.AddRange(views, view => view.Name);
            ComboBoxControl_SourceTemplate.SetSelectedItem(views[0]);
        }

        public Autodesk.Revit.DB.View SourceViewTemplate
        {
            get
            {
                return ComboBoxControl_SourceTemplate.GetSelectedItem<Autodesk.Revit.DB.View>();
            }
        }

        public FamilySymbol SourceTagType
        {
            get
            {
                return ComboBoxControl_SourceTagType.GetSelectedItem<FamilySymbol>();
            }
        }

        public Autodesk.Revit.DB.View DestinationViewTemplate
        {
            get
            {
                return ComboBoxControl_DestinationTemplate.GetSelectedItem<Autodesk.Revit.DB.View>();
            }
        }

        public FamilySymbol DestinationTagType
        {
            get
            {
                return ComboBoxControl_DestinationTagType.GetSelectedItem<FamilySymbol>();
            }
        }

        public UIDocument UIDocument
        {
            get
            {
                return uIDocument;
            }
        }

        private void ComboBoxControl_SourceTemplate_SelectedIndexChanged(object sender, EventArgs e)
        {
            FamilySymbol familySymbol = ComboBoxControl_SourceTagType.GetSelectedItem<FamilySymbol>();

            ComboBoxControl_SourceTagType.ClearItems();

            Autodesk.Revit.DB.View view = ComboBoxControl_SourceTemplate.GetSelectedItem<Autodesk.Revit.DB.View>();
            if(view == null)
            {
                return;
            }

            Document document = uIDocument?.Document;

            if (document == null)
            {
                return;
            }

            List<Autodesk.Revit.DB.View> views = new FilteredElementCollector(document).OfClass(view.GetType()).Cast<Autodesk.Revit.DB.View>().ToList();
            views.RemoveAll(x => x == null || x.IsTemplate || x.ViewType != view.ViewType || x.ViewTemplateId != view.Id);

            List<ElementFilter> elementFilters = views.ConvertAll(x => new ElementOwnerViewFilter(x.Id) as ElementFilter);

            ElementFilter elementFilter = new LogicalAndFilter(new LogicalOrFilter(new ElementCategoryFilter(BuiltInCategory.OST_MEPSpaceTags), new ElementClassFilter(typeof(IndependentTag))), new LogicalOrFilter(elementFilters));

            List<Element> elements_Tag = new FilteredElementCollector(document).WherePasses(elementFilter).ToList();
            if(elements_Tag == null || elements_Tag.Count == 0)
            {
                return;
            }

            Dictionary<ElementId, FamilySymbol> dictionary = new Dictionary<ElementId, FamilySymbol>();
            foreach(Element element in elements_Tag)
            {
                ElementId elementId = element?.GetTypeId();
                if(elementId == null || elementId == ElementId.InvalidElementId)
                {
                    continue;
                }

                if (dictionary.ContainsKey(elementId))
                    {
                    continue;
                }

                dictionary[elementId] = document.GetElement(elementId) as FamilySymbol;
            }

            ComboBoxControl_SourceTagType.AddRange(dictionary.Values, x => x.Name);
            if(!ComboBoxControl_SourceTagType.SetSelectedItem(familySymbol))
            {
                familySymbol = dictionary.Values.ToList().Find(x => x.Category.Id.IntegerValue == (int)BuiltInCategory.OST_MEPSpaceTags);
                if(familySymbol != null)
                {
                    ComboBoxControl_SourceTagType.SetSelectedItem(familySymbol);
                }
            }

        }

        private void ComboBoxControl_SourceTagType_SelectedIndexChanged(object sender, EventArgs e)
        {
            FamilySymbol familySymbol_Destination= ComboBoxControl_DestinationTagType.GetSelectedItem<FamilySymbol>();

            ComboBoxControl_DestinationTagType.ClearItems();

            FamilySymbol familySymbol_Source = ComboBoxControl_SourceTagType.GetSelectedItem<FamilySymbol>();
            if (familySymbol_Source == null)
            {
                return;
            }

            Document document = uIDocument?.Document;

            if (document == null)
            {
                return;
            }

            List<FamilySymbol> FamilySymbols = ComboBoxControl_SourceTagType.GetItems<FamilySymbol>();
            FamilySymbols.RemoveAll(x => x.Category.Id != familySymbol_Source.Category.Id);

            if (FamilySymbols == null || FamilySymbols.Count == 0)
            {
                return;
            }

            ComboBoxControl_DestinationTagType.AddRange(FamilySymbols, x => x.Name);
            if (!ComboBoxControl_DestinationTagType.SetSelectedItem(familySymbol_Destination))
            {
                familySymbol_Destination = familySymbol_Source;
                if (!ComboBoxControl_DestinationTagType.SetSelectedItem(familySymbol_Destination))
                {
                    familySymbol_Destination = FamilySymbols[0];
                    if (familySymbol_Destination != null)
                    {
                        ComboBoxControl_SourceTagType.SetSelectedItem(familySymbol_Destination);
                    }
                }
            }
        }

        private void Button_OK_Click(object sender, EventArgs e)
        {
            if(SourceViewTemplate == null)
            {
                MessageBox.Show("Please select source view template");
                return;
            }

            if (DestinationViewTemplate == null)
            {
                MessageBox.Show("Please select destination view template");
                return;
            }

            if (SourceTagType == null)
            {
                MessageBox.Show("Please select source tag type");
                return;
            }

            if (DestinationTagType == null)
            {
                MessageBox.Show("Please select destination tag type");
                return;
            }

            DialogResult = DialogResult.OK;

            Close();
        }

        private void Button_Cancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;

            Close();
        }
    }
}
