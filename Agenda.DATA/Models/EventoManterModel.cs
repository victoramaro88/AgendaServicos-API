using System;
using System.Collections.Generic;
using System.Text;

namespace Agenda.DATA.Models
{
    public class EventoManterModel
    {
        public EventoModel objEvento { get; set; }
        public List<CheckListRespostasModel> listaRespostas { get; set; }
    }
}
