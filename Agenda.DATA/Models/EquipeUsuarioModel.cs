using System;
using System.Collections.Generic;
using System.Text;

namespace Agenda.DATA.Models
{
    public class EquipeUsuarioModel
    {
        //-> Equipe
        public int equipCod { get; set; }
        public string equipDesc { get; set; }
        public bool equipStatus { get; set; }

        //-> Usuario
        public int usuCod { get; set; }
        public string usuNome { get; set; }
        public bool usuStatus { get; set; }

        //-> Perfil
        public int perfCod { get; set; }
        public string perfDesc { get; set; }
    }
}
