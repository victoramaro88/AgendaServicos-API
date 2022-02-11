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
        public int eventStatus { get; set; }
        public int horaCod { get; set; }
        public int cidaCod { get; set; }
        public int diamCod { get; set; }
        public int usuCod { get; set; }
        public int maqCod { get; set; }
        public int tipChLiCod { get; set; }

        public string horaDesc { get; set; }
        public string cidaDesc { get; set; }
        public string estSigl { get; set; }
        public string diamDesc { get; set; }
        public string usuNome { get; set; }
        public string maqMarca { get; set; }
        public string maqModelo { get; set; }
        public string tipChLiDesc { get; set; }
    }
}
