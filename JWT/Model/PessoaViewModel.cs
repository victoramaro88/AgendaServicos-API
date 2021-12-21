using System;
using System.Collections.Generic;
using System.Text;

namespace JWT.Model
{
    public class PessoaViewModel
    {
        public long pesIdf { get; set; }
        public string pesNom { get; set; }
        public string pesSclNom { get; set; }
        public short pesTipCod { get; set; }
        public DateTime pesNasDat { get; set; }
        public long efeMilCbReNum { get; set; }
        public string efeMilCbDigReNum { get; set; }
        public string efeMilCbGueNom { get; set; }
        public string posSgl { get; set; }
        public string erroMensagem { get; set; }
    }
}
