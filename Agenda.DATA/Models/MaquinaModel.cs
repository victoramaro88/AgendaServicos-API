using System;
using System.Collections.Generic;
using System.Text;

namespace Agenda.DATA.Models
{
    public class MaquinaModel
    {
        public int maqCod { get; set; }
        public string maqMarca { get; set; }
        public string maqModelo { get; set; }
        public string maqObse { get; set; }
        public bool maqStatus { get; set; }
        public int diamCod { get; set; }
        public int veicCod { get; set; }
    }
}
