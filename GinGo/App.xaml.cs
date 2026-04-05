﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;
using System.Windows;

namespace GinGo;

public partial class App : Application
{
    private bool _isDarkTheme = false;

    public void ToggleTheme()
    {
        _isDarkTheme = !_isDarkTheme;
        var themeName = _isDarkTheme ? "DarkTheme" : "LightTheme";
        var uri = new Uri($"Themes/{themeName}.xaml", UriKind.Relative);
        
        // Remove the first dictionary (which is the theme) and add the new one
        Current.Resources.MergedDictionaries[0] = new ResourceDictionary { Source = uri };
    }
}





