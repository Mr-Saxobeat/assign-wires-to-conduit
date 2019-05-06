#region Namespaces
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

#endregion

namespace AssignWiresToConduit
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            //Get app and document from Revit
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            //Get all conduits from DB
            List<Element> listConduits = new List<Element>();
            #region GetConduits
            FilteredElementCollector collector = GetConnectorElements(doc, false);
            ICollection<Element> conduits = collector.ToElements();
            foreach (Element c in conduits)
            {
                listConduits.Add(c);
            }
            #endregion

            //Array of wiringParameters that will be get from conduits
            string[] wiringParameters = new string[6];
            #region Declare wiringParameters string names
            wiringParameters[0] = "1.3. Fiação";
            wiringParameters[1] = "2.3. Fiação";
            wiringParameters[2] = "3.3. Fiação";
            wiringParameters[3] = "4.3. Fiação";
            wiringParameters[4] = "5.3. Fiação";
            wiringParameters[5] = "6.3. Fiação";
            #endregion

            //Array of gaugeParameters that will be get from conduits
            string[] gaugeParameters = new string[6];
            #region Declare gaugeParameters string names
            gaugeParameters[0] = "1.4. Bitola";
            gaugeParameters[1] = "2.4. Bitola";
            gaugeParameters[2] = "3.4. Bitola";
            gaugeParameters[3] = "4.4. Bitola";
            gaugeParameters[4] = "5.4. Bitola";
            gaugeParameters[5] = "6.4. Bitola";
            #endregion

            //Array containing the A, B and C chars
            //that will be concatenated to find 
            //a parameter.
            string[] abcPhases = new string[3];
            #region Declare ABC phases
            abcPhases[0] = "A";
            abcPhases[1] = "B";
            abcPhases[2] = "C";
            #endregion

            //
            string gaugePhaseParamName = "";

            using (Transaction t = new Transaction(doc, "AssignWiring"))
            {
                t.Start();
                //Iterates with each conduit
                //foreach (Conduit c in listConduits)
                foreach (Element c in listConduits)

                {
                    //Iterates with each wiringParameters
                    for (int i = 0; i < 6; i++)
                    {
                        //Get the wiring parameter
                        Parameter wiringParam = c.LookupParameter(wiringParameters[i]);
                        if (wiringParam.HasValue)
                        {
                            //Get the gauge parameter
                            Parameter gaugeParam = c.LookupParameter(gaugeParameters[i]);
                            if (gaugeParam.HasValue)
                            {
                                //Formats the string gauge to concatenate to the 
                                //string parameter to look up
                                string gaugeParamPrefix = gaugeParam.AsString();
                                if (gaugeParamPrefix.Contains("."))
                                {
                                    gaugeParamPrefix = gaugeParamPrefix.Replace(".", ",");
                                }
                                else if (!gaugeParamPrefix.Contains(","))
                                {
                                    gaugeParamPrefix = gaugeParamPrefix + ",0";
                                }

                                //Get the number of each phase (F, N, T, and R) the current wiring have
                                Dictionary<string, int> phases = CountPhases(wiringParam.AsString());

                                foreach (string k in phases.Keys)
                                {
                                    if (k == "F")
                                    {
                                        int nPhasesF = phases[k];
                                        //For each phase F
                                        for (int j = 0; j < nPhasesF; j++)
                                        {
                                            //The name of parameter that will be set
                                            gaugePhaseParamName =
                                                gaugeParamPrefix + "mm²_Fase " + abcPhases[j];

                                            //Get the parameter with the 
                                            //respective gauge and  phase
                                            Parameter gaugePhaseParam = c
                                                .LookupParameter(gaugePhaseParamName);

                                            if (gaugePhaseParam != null)
                                            {
                                                int currentValue = gaugePhaseParam.AsInteger();

                                                try
                                                {
                                                    gaugePhaseParam.Set(++currentValue);
                                                }
                                                catch (Exception ex)
                                                {

                                                    TaskDialog.Show("Error: ", ex.Message);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (k == "N")
                                        {
                                            //The name of parameter that will be set
                                            gaugePhaseParamName =
                                                gaugeParamPrefix + "mm²_Neutro";
                                        }
                                        else if (k == "T")
                                        {
                                            //The name of parameter that will be set
                                            gaugePhaseParamName =
                                                gaugeParamPrefix + "mm²_Terra";
                                        }
                                        else if (k == "R")
                                        {
                                            //The name of parameter that will be set
                                            gaugePhaseParamName =
                                                gaugeParamPrefix + "mm²_Retorno";
                                        }

                                        //Get the parameter with the 
                                        //respective gauge and  phase
                                        Parameter gaugePhaseParam = c
                                            .LookupParameter(gaugePhaseParamName);

                                        
                                        if (gaugePhaseParam != null)
                                        {
                                            int currentValue = gaugePhaseParam.AsInteger();

                                            try
                                            {
                                                gaugePhaseParam.Set(currentValue + phases[k]);
                                            }
                                            catch (Exception ex)
                                            {

                                                TaskDialog.Show("Error: ", ex.Message);
                                            }
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
                t.Commit();
            }
            return Result.Succeeded;
        }


        //Returns a dictionary containing the phases and 
        //your quantity that run inside the conduit.
        private Dictionary<string, int> CountPhases(string wiring)
        {
            Dictionary<string, int> phases = new Dictionary<string, int>();

            foreach (char c in wiring)
            {
                string key = c.ToString();

                if (!phases.ContainsKey(key))
                {
                    phases.Add(key, 1);
                }
                else
                {
                    phases[key] = phases[key] + 1;
                }
            }

            return phases;
        }

        //I was having trouble with FilteredELementCollector to
        //retrieve "Conduit Fittings", so a got this code that worked for me.
        //--------------------------------------------------------------------
        //Returns a FilteredElementCollector with the connector elements
        //https://thebuildingcoder.typepad.com/blog/2010/06/retrieve-mep-elements-and-connectors.html
        //Jeremy Tammik
        static FilteredElementCollector GetConnectorElements(
            Document doc,
            bool include_wires)
        {
            // what categories of family instances
            // are we interested in?

            BuiltInCategory[] bics = new BuiltInCategory[] {
                //BuiltInCategory.OST_CableTray,
                //BuiltInCategory.OST_CableTrayFitting,
                BuiltInCategory.OST_Conduit,
                BuiltInCategory.OST_ConduitFitting,
                //BuiltInCategory.OST_DuctCurves,
                //BuiltInCategory.OST_DuctFitting,
                //BuiltInCategory.OST_DuctTerminal,
                //BuiltInCategory.OST_ElectricalEquipment,
                //BuiltInCategory.OST_ElectricalFixtures,
                //BuiltInCategory.OST_LightingDevices,
                //BuiltInCategory.OST_LightingFixtures,
                //BuiltInCategory.OST_MechanicalEquipment,
                //BuiltInCategory.OST_PipeCurves,
                //BuiltInCategory.OST_PipeFitting,
                //BuiltInCategory.OST_PlumbingFixtures,
                //BuiltInCategory.OST_SpecialityEquipment,
                //BuiltInCategory.OST_Sprinklers,
                //BuiltInCategory.OST_Wire,
            };

            IList<ElementFilter> a
              = new List<ElementFilter>(bics.Count());

            foreach (BuiltInCategory bic in bics)
            {
                a.Add(new ElementCategoryFilter(bic));
            }

            LogicalOrFilter categoryFilter
              = new LogicalOrFilter(a);

            LogicalAndFilter familyInstanceFilter
              = new LogicalAndFilter(categoryFilter,
                new ElementClassFilter(
                  typeof(FamilyInstance)));

            IList<ElementFilter> b
              = new List<ElementFilter>(6);

            b.Add(new ElementClassFilter(typeof(CableTray)));
            b.Add(new ElementClassFilter(typeof(Conduit)));
            b.Add(new ElementClassFilter(typeof(Duct)));
            b.Add(new ElementClassFilter(typeof(Pipe)));
            
            if (include_wires)
            {
                b.Add(new ElementClassFilter(typeof(Wire)));
            }

            b.Add(familyInstanceFilter);

            LogicalOrFilter classFilter
              = new LogicalOrFilter(b);

            FilteredElementCollector collector
              = new FilteredElementCollector(doc);

            collector.WherePasses(classFilter);

            return collector;
        }
    }
}
