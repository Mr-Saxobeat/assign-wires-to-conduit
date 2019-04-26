#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
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

            List<Conduit> listConduits = new List<Conduit>();
            //Get all conduits from DB
            #region GetConduits
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> conduits = collector.OfClass(typeof(Conduit)).ToElements();

            //Add conduits elements to listConduits
            foreach (Conduit c in conduits)
            {
                listConduits.Add(c);
            }
            #endregion

            //This dict remains the "wiring" as key and the gauge as value
            Dictionary<string, double> wireGaugePairs = new Dictionary<string, double>();

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

            //Iterates with each conduit
            foreach (Conduit c in listConduits)
            {
                //Iterates with each wiringParameters
                for (int i = 0; i < 6; i++)
                {
                    Parameter wiringParam = c.LookupParameter(wiringParameters[i]);
                    if (wiringParam.HasValue)
                    {
                        Parameter gaugeParam = c.LookupParameter(gaugeParameters[i]);
                        if(gaugeParam.HasValue)
                        {
                            int nPhases = CountPhases(wiringParam.ToString());    
                        }
                    }
                }
            }

            return Result.Succeeded;
        }

        //Return the number of phases of the 
        //given wiring configurantion.
        private int CountPhases(string wiring)
        {
            int nPhases = 0;

            foreach (char c in wiring)
            {
                if (c.ToString() == "F")
                {
                    nPhases++;
                }
            }

            return nPhases;
        }
    }
}
