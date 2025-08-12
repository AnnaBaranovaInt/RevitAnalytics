using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics
{
    public enum Restrains
    {
        Fixed,
        Pinned,
        Roller,
        NoRestraint
    }
  public  class SapJointRestraint
    {
        private bool[] restrains;

        public bool[] Restrains
        {
            get { return restrains; }
            set { restrains= value; }
        }

        #region Constructions

        public SapJointRestraint(Restrains RestrainType)
        {
            restrains = new bool[6];
            switch (RestrainType)
            {
                case RevitAnalytics.Restrains.Fixed:
                    for (int i = 0; i <6; i++)
                    {
                        restrains[i] = true;
                    }
                    break;

                case RevitAnalytics.Restrains.Pinned:
                    for (int i = 0; i < 3; i++)
                    {
                        restrains[i] = true;
                    }
                    break;
                case RevitAnalytics.Restrains.Roller:
                    //set constrains to Z-Axis
                    restrains[2] = true;
                    break;
                case RevitAnalytics.Restrains.NoRestraint:

                    break;
                default:
                    break;
            }
            

        }
        public void SetRestraint(bool U1,bool U2,bool U3,bool R1,bool R2,bool R3)
        {
            restrains[0] = U1;
            restrains[1] = U2;
            restrains[2] = U3;
            restrains[3] = R1;
            restrains[4] = R2;
            restrains[5] = R3;
        }
        #endregion


       
    }
}
