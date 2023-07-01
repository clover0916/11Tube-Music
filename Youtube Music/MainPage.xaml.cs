using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.WebUI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Youtube_Music
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        bool is_first;
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void WebView_NavigationCompleted(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
        {
            //MainWindow‚ÌNavigationViewControl‚ðŽó‚¯Žæ‚é
            MainWindow mainWindow = (MainWindow)Window.Current;

            if (mainWindow != null)
            {
                mainWindow.Title = "aaaaaa";
            }

            

            loadingBar.Visibility = Visibility.Collapsed;
            WebView.Visibility = Visibility.Visible;
            await Task.Delay(500);
            WebView.Opacity = 1;
            await WebView.ExecuteScriptAsync(@"
                const hideElements = [
                    { tabId: 'SPunlimited' },
                    { tabId: 'FEmusic_home' },
                    { tabId: 'FEmusic_explore' },
                    { tabId: 'FEmusic_library_landing' }
                ];

                hideElements.forEach(element => {
                    const elements = document.querySelectorAll('[tab-id=' + element.tabId + ']');
                    elements.forEach(element => {
                        element.style.display = 'none';
                    });
                });
            ");
        }

        public async void ControlWebView(string tag)
        {
            if (tag == "home")
            {
                await WebView.ExecuteScriptAsync("document.querySelector('[tab-id=\"FEmusic_home\"]').click();");
            } else if (tag == "explore")
            {
                await WebView.ExecuteScriptAsync("document.querySelector('[tab-id=\"FEmusic_explore\"]').click();");
            } else if (tag == "library")
            {
                await WebView.ExecuteScriptAsync("document.querySelector('[tab-id=\"FEmusic_library_landing\"]').click();");
            }
        }

        private void WebView_NavigationStarting(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
        {
            string script = @"
                {
	let pruner = function (o) {
		delete o.playerAds;
		delete o.adPlacements;
		//
		if (o.playerResponse) {
			delete o.playerResponse.playerAds;
			delete o.playerResponse.adPlacements;
		}
		//
		return o;
	};

	JSON.parse = new Proxy(JSON.parse, {
		apply: function () {
			return pruner(Reflect.apply(...arguments));
		},
	});

	Response.prototype.json = new Proxy(Response.prototype.json, {
		apply: function () {
			return Reflect.apply(...arguments).then((o) => pruner(o));
		},
	});
}

(function () {
	let cValue = ""undefined"";
	const chain = ""playerResponse.adPlacements"";
	const thisScript = document.currentScript;
	//
	if (cValue === ""null"") cValue = null;
	else if (cValue === ""''"") cValue = """";
	else if (cValue === ""true"") cValue = true;
	else if (cValue === ""false"") cValue = false;
	else if (cValue === ""undefined"") cValue = undefined;
	else if (cValue === ""noopFunc"") cValue = function () {};
	else if (cValue === ""trueFunc"")
		cValue = function () {
			return true;
		};
	else if (cValue === ""falseFunc"")
		cValue = function () {
			return false;
		};
	else if (/^\d+$/.test(cValue)) {
		cValue = parseFloat(cValue);
		//
		if (isNaN(cValue)) return;
		if (Math.abs(cValue) > 0x7fff) return;
	} else {
		return;
	}
	//
	let aborted = false;
	const mustAbort = function (v) {
		if (aborted) return true;
		aborted =
			v !== undefined &&
			v !== null &&
			cValue !== undefined &&
			cValue !== null &&
			typeof v !== typeof cValue;
		return aborted;
	};

	/*
	Support multiple trappers for the same property:
	https://github.com/uBlockOrigin/uBlock-issues/issues/156
  */

	const trapProp = function (owner, prop, configurable, handler) {
		if (handler.init(owner[prop]) === false) {
			return;
		}
		//
		const odesc = Object.getOwnPropertyDescriptor(owner, prop);
		let prevGetter, prevSetter;
		if (odesc instanceof Object) {
			if (odesc.configurable === false) return;
			if (odesc.get instanceof Function) prevGetter = odesc.get;
			if (odesc.set instanceof Function) prevSetter = odesc.set;
		}
		//
		Object.defineProperty(owner, prop, {
			configurable,
			get() {
				if (prevGetter !== undefined) {
					prevGetter();
				}
				//
				return handler.getter();
			},
			set(a) {
				if (prevSetter !== undefined) {
					prevSetter(a);
				}
				//
				handler.setter(a);
			},
		});
	};

	const trapChain = function (owner, chain) {
		const pos = chain.indexOf(""."");
		if (pos === -1) {
			trapProp(owner, chain, false, {
				v: undefined,
				getter: function () {
					return document.currentScript === thisScript ? this.v : cValue;
				},
				setter: function (a) {
					if (mustAbort(a) === false) return;
					cValue = a;
				},
				init: function (v) {
					if (mustAbort(v)) return false;
					//
					this.v = v;
					return true;
				},
			});
			//
			return;
		}
		//
		const prop = chain.slice(0, pos);
		const v = owner[prop];
		//
		chain = chain.slice(pos + 1);
		if (v instanceof Object || (typeof v === ""object"" && v !== null)) {
			trapChain(v, chain);
			return;
		}
		//
		trapProp(owner, prop, true, {
			v: undefined,
			getter: function () {
				return this.v;
			},
			setter: function (a) {
				this.v = a;
				if (a instanceof Object) trapChain(a, chain);
			},
			init: function (v) {
				this.v = v;
				return true;
			},
		});
	};
	//
	trapChain(window, chain);
})();

(function () {
	let cValue = ""undefined"";
	const thisScript = document.currentScript;
	const chain = ""ytInitialPlayerResponse.adPlacements"";
	//
	if (cValue === ""null"") cValue = null;
	else if (cValue === ""''"") cValue = """";
	else if (cValue === ""true"") cValue = true;
	else if (cValue === ""false"") cValue = false;
	else if (cValue === ""undefined"") cValue = undefined;
	else if (cValue === ""noopFunc"") cValue = function () {};
	else if (cValue === ""trueFunc"")
		cValue = function () {
			return true;
		};
	else if (cValue === ""falseFunc"")
		cValue = function () {
			return false;
		};
	else if (/^\d+$/.test(cValue)) {
		cValue = parseFloat(cValue);
		//
		if (isNaN(cValue)) return;
		if (Math.abs(cValue) > 0x7fff) return;
	} else {
		return;
	}
	//
	let aborted = false;
	const mustAbort = function (v) {
		if (aborted) return true;
		aborted =
			v !== undefined &&
			v !== null &&
			cValue !== undefined &&
			cValue !== null &&
			typeof v !== typeof cValue;
		return aborted;
	};

	/*
	Support multiple trappers for the same property:
	https://github.com/uBlockOrigin/uBlock-issues/issues/156
  */

	const trapProp = function (owner, prop, configurable, handler) {
		if (handler.init(owner[prop]) === false) {
			return;
		}
		//
		const odesc = Object.getOwnPropertyDescriptor(owner, prop);
		let prevGetter, prevSetter;
		if (odesc instanceof Object) {
			if (odesc.configurable === false) return;
			if (odesc.get instanceof Function) prevGetter = odesc.get;
			if (odesc.set instanceof Function) prevSetter = odesc.set;
		}
		//
		Object.defineProperty(owner, prop, {
			configurable,
			get() {
				if (prevGetter !== undefined) {
					prevGetter();
				}
				//
				return handler.getter();
			},
			set(a) {
				if (prevSetter !== undefined) {
					prevSetter(a);
				}
				//
				handler.setter(a);
			},
		});
	};

	const trapChain = function (owner, chain) {
		const pos = chain.indexOf(""."");
		if (pos === -1) {
			trapProp(owner, chain, false, {
				v: undefined,
				getter: function () {
					return document.currentScript === thisScript ? this.v : cValue;
				},
				setter: function (a) {
					if (mustAbort(a) === false) return;
					cValue = a;
				},
				init: function (v) {
					if (mustAbort(v)) return false;
					//
					this.v = v;
					return true;
				},
			});
			//
			return;
		}
		//
		const prop = chain.slice(0, pos);
		const v = owner[prop];
		//
		chain = chain.slice(pos + 1);
		if (v instanceof Object || (typeof v === ""object"" && v !== null)) {
			trapChain(v, chain);
			return;
		}
		//
		trapProp(owner, prop, true, {
			v: undefined,
			getter: function () {
				return this.v;
			},
			setter: function (a) {
				this.v = a;
				if (a instanceof Object) trapChain(a, chain);
			},
			init: function (v) {
				this.v = v;
				return true;
			},
		});
	};
	//
	trapChain(window, chain);
})();
            ";

            sender.CoreWebView2.ExecuteScriptAsync(script);
        }
    }
}
