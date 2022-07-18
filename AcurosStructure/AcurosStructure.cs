using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

// TODO: Replace the following version attributes by creating AssemblyInfo.cs. You can do this in the properties of the Visual Studio project.
[assembly: AssemblyVersion("1.0.0.1")]

// TODO: Uncomment the following line if the script requires write access.
[assembly: ESAPIScript(IsWriteable = true)]

namespace VMS.TPS
{
  public class Script
  {
    public Script()
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext context /*, System.Windows.Window window, ScriptEnvironment environment*/)
        {
            // TODO : Add here the code that is called when the script is launched from Eclipse.

            Patient patient = context.Patient;
            patient.BeginModifications();
            StructureSet ss = context.StructureSet;
            //I will use the copy to find my HU-Pixels with CreateAndSearchBody without running in approval issues
            StructureSet ss_Copy = ss.Copy();
           
            if (ss == null)
            {
                MessageBox.Show("No StructureSet was found. Script will close now.");
                return;
            }
            Image image = ss.Image;

            
            Structure newStr = ss.AddStructure("Control", "zAcuros");

            Structure body = ss_Copy.Structures.Where(x => !x.IsEmpty && (x.DicomType.ToUpper().Equals("EXTERNAL") || x.DicomType.ToUpper().Equals("BODY") || x.Id.ToUpper().Equals("KÖRPER") || x.Id.ToUpper().Equals("BODY") || x.Id.ToUpper().Contains("OUTER CONTOUR"))).FirstOrDefault();

            for (int k = 0; k < image.ZSize; k++)
            {
                body.ClearAllContoursOnImagePlane(k);
            }
            //I did not understand how to create SearchBodyParameters from scratch but changing the default is easy
            var BodyPar = ss_Copy.GetDefaultSearchBodyParameters();
            //Interesting to see
            /*MessageBox.Show("PreCloseOpenings_"+BodyPar.PreCloseOpenings.ToString()+"\n" +
                "FillAllCavity_" + BodyPar.FillAllCavities.ToString() + "\n" +
                "KeepLargestParts_" + BodyPar.KeepLargestParts.ToString() + "\n" +
                "LowerHU_" + BodyPar.LowerHUThreshold.ToString() + "\n" +
                "MRedgeHigh_" + BodyPar.MREdgeThresholdHigh.ToString() + "\n" +
                "MRedgeLow_" + BodyPar.MREdgeThresholdLow.ToString() + "\n" +
                "NumberOfLargestParts_" + BodyPar.NumberOfLargestPartsToKeep.ToString() + "\n" +
                "PreCloseOpeningRadius_" + BodyPar.PreCloseOpeningsRadius.ToString() + "\n" +
                "PreDisconnect_" + BodyPar.PreDisconnect.ToString() + "\n" +
                "PreDisconnectRadius_" + BodyPar.PreDisconnectRadius.ToString() + "\n" +
                "Smoothing_" + BodyPar.Smoothing.ToString() + "\n" +
                "SmoothingLevel_" + BodyPar.SmoothingLevel.ToString() 
                );*/
            BodyPar.PreCloseOpenings = false;
            BodyPar.FillAllCavities = false;
            BodyPar.PreDisconnect = false;
            BodyPar.Smoothing = false;
            //for a directDensity CT this is the appropiate value. For other PlanningCTs it should be higher
            BodyPar.LowerHUThreshold = 1600;

            ss_Copy.CreateAndSearchBody(BodyPar);
            newStr.SegmentVolume = body.SegmentVolume;
            Image imageDelete = ss_Copy.Image;
            ss_Copy.Delete();
            //images cannot be deleted with ESAPI. Therefore I will rename it
            imageDelete.Id = "IgnoreOrDelete";

            string errorMessage = "";
            if (newStr.CanSetAssignedHU(out errorMessage).ToString().ToLower() == "true")
            {
                newStr.SetAssignedHU(1600);
                MessageBox.Show(string.Format("New Structure with following parameters was created:\n\n" +
                    "Id:\t{0}\n" +
                    "Volume:\t{1}\n" +
                    "SeperateParts:\t{2}\n" +
                    "AssignedHU:\t{3}"
                    , newStr.Id, Math.Round(newStr.Volume, 3), newStr.GetNumberOfSeparateParts(), 1600, "Success"));
            }
            else
            {
                MessageBox.Show(string.Format("New Structure with following parameters was created:\n\n" +
                    "Id:\t{0}\n" +
                    "Volume:\t{1}\n" +
                    "SeperateParts:\t{2}\n" +
                    "AssignedHU:\t{3}\n\n" +
                    "Assign-Error:\t{4}"
                    , newStr.Id, Math.Round(newStr.Volume, 3), newStr.GetNumberOfSeparateParts(), "No", errorMessage), "Success");
            }
            /*
            // First try using VVectors
            VVector[] newcontour = new VVector[50];

            int[,] buffer = new int[image.XSize, image.YSize];
            double[,] hu = new double[image.XSize, image.YSize];
            int n;
            for (int k = 0; k < 1; k++)
            {
                n = 0;
                image.GetVoxels(k, buffer);
                for (int i = 0; i < image.XSize; i++)
                {
                    for (int j = 0; j < image.YSize; j++)
                    {
                        hu[i, j] = image.VoxelToDisplayValue(buffer[i, j]);
                        if (hu[i, j] < -0 && n<50)
                        {
                            newcontour[n] = new VVector(image.Origin.x+i, image.Origin.y+j, image.Origin.z+k);
                            n = n + 1;
                        }
                    }
                }
                MessageBox.Show(n.ToString());
                newStr.AddContourOnImagePlane(newcontour, k);
            }*/

        }
    }
}
