using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRetainerAPI.Configuration;
[Serializable]
public unsafe sealed class GCPurchasePlan
{
    public Guid GUID = Guid.NewGuid();
    public string ID;

    public GCPurchasePlan()
    {
        this.ID = GUID.ToString();
    }
}