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
	let cValue = "undefined";
	const chain = "playerResponse.adPlacements";
	const thisScript = document.currentScript;
	//
	if (cValue === "null") cValue = null;
	else if (cValue === "''") cValue = "";
	else if (cValue === "true") cValue = true;
	else if (cValue === "false") cValue = false;
	else if (cValue === "undefined") cValue = undefined;
	else if (cValue === "noopFunc") cValue = function () { };
	else if (cValue === "trueFunc")
		cValue = function () {
			return true;
		};
	else if (cValue === "falseFunc")
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
		const pos = chain.indexOf(".");
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
		if (v instanceof Object || (typeof v === "object" && v !== null)) {
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
	let cValue = "undefined";
	const thisScript = document.currentScript;
	const chain = "ytInitialPlayerResponse.adPlacements";
	//
	if (cValue === "null") cValue = null;
	else if (cValue === "''") cValue = "";
	else if (cValue === "true") cValue = true;
	else if (cValue === "false") cValue = false;
	else if (cValue === "undefined") cValue = undefined;
	else if (cValue === "noopFunc") cValue = function () { };
	else if (cValue === "trueFunc")
		cValue = function () {
			return true;
		};
	else if (cValue === "falseFunc")
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
		const pos = chain.indexOf(".");
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
		if (v instanceof Object || (typeof v === "object" && v !== null)) {
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