using System;
using System.Collections.Generic;
using System.Text;

namespace Agenda.DATA.Models
{
    public class EventoModel
    {
        public int eventCod { get; set; }
        public string eventDesc { get; set; }
        public string eventLogr { get; set; }
        public string eventBairr { get; set; }
        public DateTime eventDtIn { get; set; }
        public DateTime evenDtFi { get; set; }
        public string eventObse { get; set; }
        public bool eventStatus { get; set; }
        public int horaCod { get; set; }
        public int cidaCod { get; set; }
        public int diamCod { get; set; }
        public int usuCod { get; set; }
        public int maqCod { get; set; }
    }
}
