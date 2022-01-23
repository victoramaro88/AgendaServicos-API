using System;
using System.Collections.Generic;
using System.Text;

namespace Agenda.DATA.Models
{
    public class ChecklistEnvioModel
    {
        public ChecklistModel objChecklist { get; set; }
        public List<ItemCheckListModel> listaItemChecklist { get; set; }
    }
}
