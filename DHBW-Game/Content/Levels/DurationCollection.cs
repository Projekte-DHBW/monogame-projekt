using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;


namespace DHBW_Game.UI;

public class DurationCollection
{
    public List<double> Durations = new List<double>();
    public DurationCollection()
    {
        Durations.Add(45);
        Durations.Add(40);
        Durations.Add(60);
    }
}