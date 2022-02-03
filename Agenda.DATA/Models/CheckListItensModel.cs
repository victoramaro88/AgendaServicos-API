using System;
using System.Collections.Generic;
using System.Text;

namespace Agenda.DATA.Models
{
    public class CheckListItensModel
    {
        public int chkLstItmChkLst { get; set; }
        public int chLsCod { get; set; }
        public int itmChLsCod { get; set; }
        public string itmChLsDesc { get; set; }
        public bool itmChLsObrig { get; set; }
        public bool itmChLsStatus { get; set; }
    }
}
