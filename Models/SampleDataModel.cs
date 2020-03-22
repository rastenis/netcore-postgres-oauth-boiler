using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace netcore_postgres_oauth_boiler.Models
{
    public class SampleDataListModel
    {
        public SampleDataListModel()
        {
            this.SampleList = new List<SampleDataModel>();
        }

        public List<SampleDataModel> SampleList;
    }

    public class SampleDataModel
    {
        public SampleDataModel(string name, string address)
        {
            this.Name = name;
            this.Address = address;
        }

        public string Name;
        public string Address;
    }
}
