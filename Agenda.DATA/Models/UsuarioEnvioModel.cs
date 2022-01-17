using System;
using System.Collections.Generic;
using System.Text;

namespace Agenda.DATA.Models
{
    public class UsuarioEnvioModel
    {
        public EquipeModel objEnvioEquipe { get; set; }
        public List<UsuarioTbModel> objEnvioListaUsuario { get; set; }
    }
}
