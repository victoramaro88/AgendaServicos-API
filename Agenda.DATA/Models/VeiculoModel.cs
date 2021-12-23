using System;
using System.Collections.Generic;
using System.Text;

namespace Agenda.DATA.Models
{
    public class VeiculoModel
    {
        public int veicCod { get; set; }
        public string veicMarca { get; set; }
        public string veicModelo { get; set; }
        public int veicAno { get; set; }
        public string veicPlaca { get; set; }
        public string veicObse { get; set; }
        public bool veicStatus { get; set; }
        public int tipVeicCod { get; set; }
    }
}
