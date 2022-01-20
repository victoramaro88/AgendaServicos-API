using System;
using System.Collections.Generic;
using System.Text;

namespace Agenda.DATA.Models
{
    public class ChecklistModel
    {
        public int chLsCod { get; set; }
        public string chLsDesc { get; set; }
        public bool chLsStatus { get; set; }
        public int tipChLiCod { get; set; } 
    }
}
