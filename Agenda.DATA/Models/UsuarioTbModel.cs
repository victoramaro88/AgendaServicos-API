using System;
using System.Collections.Generic;
using System.Text;

namespace Agenda.DATA.Models
{
    public class UsuarioTbModel
    {
        public int usuCod { get; set; }
        public string usuNome { get; set; }
        public string usuLogin { get; set; }
        public string usuSenha { get; set; }
        public bool usuStatus { get; set; }
        public int perfCod { get; set; }
    }
}
