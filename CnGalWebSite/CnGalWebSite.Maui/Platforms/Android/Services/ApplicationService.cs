﻿using Android.App;
using Microsoft.Maui.Controls.Compatibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CnGalWebSite.Maui.Services
{ 
    public partial class ApplicationService
    {
        public void CloseApplication()
        {
            var activity = Platform.CurrentActivity;
            activity.FinishAffinity();
        }
    }
}
