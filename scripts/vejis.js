/*
    VEJIS JavaScript Framework v0.5.0.21
    http://vejis.org

    This version is still preliminary and subject to change.
    
    Copyright 2012, VILIC VANE
    Licensed under the MIT license.
*/

"VEJIS",
function () {
    if (typeof intellisense != "undefined" && typeof _ != "undefined")
        return;

    /* COMMON VARIABLES */

    var global = function () { return this; }();
    var hasOwnProperty = Object.prototype.hasOwnProperty;
    var slice = Array.prototype.slice;

    /* DEBUG STUFFS */

    "Override Error",
    function () {
        var OriginalError = global.Error;
        OriginalError.stackTraceLimit = Infinity;

        var e = new OriginalError();
        if (!e.stack) return;

        var re = /(?: +at +(?:\S+ +)?|@)\(?([^\(\)\s]+?):\d+(?::\d+)?\)?\s*$/;

        var stackREStr = (re.exec(e.stack) || ["", ""])[1].replace(/([^a-z0-9])/gi, "\\$1");
        if (!stackREStr) return;

        stackREStr = "(?: +at +(?:\\S+ +)?|@)\\(?(?!" + stackREStr + ":)([^\\(\\)\\s]+?):(\\d+)(?::(\\d+))?\\)? *(?=\r?\n|$)";

        var stackRE = new RegExp(stackREStr, "g");
        var stackDetailRE = new RegExp(stackREStr);

        global.Error = function (message) {
            var e = new OriginalError();
            var details = stackDetailRE.exec(e.stack);

            var stack = e.stack.replace(/^@/mg, "    at ");
            var lines = stack.match(stackRE);

            if (!details || !lines)
                return e;

            e.message =
            (message || "") + "\n" +
            "    position " + details[1] + ":" + details[2] + (details[3] ? ":" + details[3] : "") + "\n" +
            "Call stack:\n" +
            lines.join("\n");

            e.fileName = details[1];
            e.lineNumber = Number(details[2]);

            return e;
        };

        global.Error.prototype = new OriginalError();
    }();

    /* COMMON METHODS */
    function log(msg) {
        if (global.console)
            console.log(new Error(msg).message);
    }

    function warn(msg) {
        if (global.console)
            console.warn(new Error(msg).message);
    }

    function copy(from, to, overwrite) {
        if (overwrite) {
            for (var i in from) {
                if (hasOwnProperty.call(from, i))
                    to[i] = from[i];
            }
        }
        else {
            for (var i in from) {
                if (hasOwnProperty.call(from, i) && !hasOwnProperty.call(to, i))
                    to[i] = from[i];
            }
        }
    }

    function createStringMap() {
        var map = {};
        var m = function (key, value) {
            if (arguments.length == 2)
                map[key] = value;
            return hasOwnProperty.call(map, key) ? map[key] : undefined;
        };

        return m;
    }

    function is_(object, Type) {
        if (object && object.__relatedInstance__)
            object = object.__relatedInstance__;

        if (Type.__isInstance__)
            return Type.__isInstance__(object);

        if (object != null && Type === Object)
            return true;

        switch (typeof object) {
            case "object":
            case "function":
            case "undefined":
                return object instanceof Type;
            default:
                //for string, number and boolean
                return new object.constructor() instanceof Type;
        }
    }

    function isType(Type) {
        return (
            typeof Type == "function" ||
            typeof Type == "object" && !!Type && !!Type.__isInstance__
        );
    }

    function isTypeMark(TypeMark) {
        return (
            typeof TypeMark == "object" &&
            !!TypeMark && typeof TypeMark.__type__ == "number"
        );
    }

    function isTypeEqual(TypeA, TypeB) {
        return (
            TypeA == TypeB ||
            TypeA.__type__ == ParamType.delegate && (TypeB.__type__ == ParamType.delegate || TypeB == Function) ||
            TypeA == Function && TypeB.__type__ == ParamType.delegate ||
            isTypeMark(TypeA) && isTypeMark(TypeB) &&
            TypeA.__type__ == TypeB.__type__ &&
            TypeA.RelatedType == TypeB.RelatedType
        );
    }

    /* VEJIS METHOD OVERLOADING */

    var ParamType = {
        normal: 0,
        option: 1,
        params: 2,
        delegate: 3
    };

    function opt_(Type, defaultValue) {
        if (!arguments.length)
            throw new Error("no argument given.");

        if (!isType(Type))
            throw new Error('argument "Type" given is invalid.');

        if (arguments.length == 1) {
            switch (Type) {
                case Number:
                case Integer:
                    defaultValue = 0;
                    break;
                case String:
                    defaultValue = "";
                    break;
                case Boolean:
                    defaultValue = false;
                    break;
                case Object:
                case PlainObject:
                    defaultValue = {};
                    break;
                case Array:
                    defaultValue = [];
                    break;
                default:
                    if (Type.__relatedType__ == List)
                        defaultValue = [];
                    else if (Type.__nullable__)
                        defaultValue = null;
                    else
                        throw new Error('argument "defaultValue" is required when the "Type" given is not String, Number, Boolean or a nullable type.');
                    break;
            }
        }
        else if (arguments.length > 1 && !is_(defaultValue, Type))
            throw new Error('arguments "defaultValue" and "Type" given doesn\'t match.');

        return {
            __type__: ParamType.option,
            RelatedType: Type,
            defaultValue: defaultValue,
            __name__: getTypeName(Type)
        };
    }

    function params_(Type) {
        if (!arguments.length)
            throw new Error("no argument given.");

        if (!isType(Type))
            throw new Error('argument "Type" given is invalid.');

        return {
            __type__: ParamType.params,
            RelatedType: Type,
            __name__: getTypeName(Type)
        };
    }

    function nul_(Type) {
        if (!arguments.length)
            throw new Error("no argument given.");

        if (!isType(Type))
            throw new Error('argument "Type" given is invalid.');

        return {
            __type__: ParamType.normal,
            __nullable__: true,
            __isInstance__: function (object) {
                return object == null || is_(object, Type); // returns true when object is undefined.
            },
            __name__: getTypeName(Type) + "?"
        };
    }

    function delegate_(name, Types, body) {
        var dName = "";

        var i = 0;
        if (is_(arguments[i], String)) {
            dName = arguments[i];
            i++;
        }

        var ParamTypes;
        var fn;

        if (is_(arguments[i], Array))
            ParamTypes = arguments[i++];
        else {
            var typesEnd = arguments.length - 1;
            ParamTypes = [];
            for (i; i < typesEnd; i++)
                ParamTypes.push(arguments[i]);
        }

        if (arguments.length <= i)
            throw new Error('"fn" is missing.');

        fn = arguments[i];

        var typeNames = [];
        for (i = 0; i < ParamTypes.length; i++) {
            var Type = ParamTypes[i];
            if (!isTypeMark(Type) && !isType(Type))
                throw new Error("invalid parameter type.");
            typeNames.push(getTypeName(Type));
        }

        if (fn == null)
            fn = function () { };

        if (typeof fn != "function")
            throw new Error('"fn" should be null or a function.');

        var names = fn.toString().match(/\((.*?)\)/)[1].match(/[^,\s]+/g) || [];
        for (i = 0; i < typeNames.length; i++)
            typeNames[i] += " " + (names[i] || "p" + (i + 1));

        var delegate = {
            __type__: ParamType.delegate,
            __RelatedTypes__: ParamTypes,
            __isInstance__: function (object) {
                return typeof object == "function";
            },
            __name__: (dName || "delegate") + "(" + typeNames.join(", ") + ")",
            with_: function (Type) {
                if (!isType(Type))
                    throw new Error('argument "Type" given is invalid.');
                delegate.__RelatedThisType__ = Type;
                delete delegate.with_;
                delete delegate.bind_;
                return delegate;
            },
            bind_: function (object) {
                if (!is_(object, Object))
                    throw new Error("object required.");
                delegate.__relatedThisObject__ = { value: object };
                delete delegate.with_;
                delete delegate.bind_;
                return delegate;
            },
            as_: function (Type) {
                if (!isType(Type))
                    throw new Error('argument "Type" given is invalid.');
                delegate.__RelatedReturnType__ = Type;
                delegate.__name__ = getTypeName(Type) + " " + delegate.__name__;
                delete delegate.as_;
                return delegate;
            }
        };

        return delegate;
    }

    function _(name, Types, body) {
        var collection; 

        var method = function () {
            return collection.exec(this, arguments);
        };

        var args;

        if (is_(name, String)) {
            args = slice.call(arguments, 1);
            method.__name__ = name;
        }
        else args = arguments;

        collection = new OverloadCollection(method);

        method._ = function (Types, body) {
            if (arguments.length == 0)
                throw new Error("body is required.");

            var ParamTypes = [];
            var typesLength = arguments.length - 1;
            for (var i = 0; i < typesLength; i++) {
                var Type = arguments[i];
                if (!isTypeMark(Type) && !isType(Type))
                    throw new Error("invalid parameter type.");
                ParamTypes.push(Type);
            }

            var fn = arguments[typesLength];

            if (typeof fn != "function")
                throw new Error('"body" must be a function');

            var overload = new Overload(ParamTypes, fn);

            collection.add(overload);

            method.as_ = function (Type) {
                if (!isType(Type))
                    throw new Error('argument "Type" given is invalid.');
                overload.ReturnType = Type;
                delete method.as_;
                return method;
            };

            method.with_ = function (Type) {
                if (!isType(Type))
                    throw new Error('argument "Type" given is invalid.');
                overload.ThisType = Type;
                delete method.with_;
                delete method.bind_;
                return method;
            };

            method.bind_ = function (object) {
                if (!is_(object, Object))
                    throw new Error("object required.");
                overload.thisObject = { value: object };
                delete method.with_;
                delete method.bind_;
                return method;
            };

            method.static_ = function (staticObject) {
                if (!is_(staticObject, PlainObject))
                    throw new Error('"staticObject" must be a plain object.');
                overload.staticObject = staticObject;

                for (var i = 0; i < params.length; i++)
                    params[i].name = names[i + 1];

                delete method.static_;
                return method;
            };

            return method;
        };

        method.toString = function () {
            return collection.toString();
        };

        if (args.length > 0)
            method._.apply(method, args);

        return method;
    }

    function getTypeName(Type, def) {
        var name;

        if (Type.__name__)
            name = Type.__name__;
        else if (typeof Type == "function")
            name = /^function\s([^\(]*)/.exec(Type.toString())[1];

        if (typeof name != "string")
            name = arguments.length == 1 ? "UnnamedType" : def;

        return name;
    }

    function wrapDelegate(fn, Delegate) {
        fn = _.apply(null, Delegate.__RelatedTypes__.concat(fn));
        if (Delegate.__relatedThisObject__)
            fn.bind_(Delegate.__relatedThisObject__.value);
        else if (Delegate.__RelatedThisType__)
            fn.with_(Delegate.__RelatedThisType__);
        if (Delegate.__RelatedReturnType__)
            fn.as_(Delegate.__RelatedReturnType__);
        return fn;
    }

    global._ = _;
    global.opt_ = opt_;
    global.params_ = params_;
    global.nul_ = nul_;
    global.delegate_ = delegate_;

    function Overload(Types, fn) {
        this.Types = Types;
        this.thisObject = undefined;
        this.ThisType = undefined;
        this.ReturnType = undefined;
        this.staticObject = undefined;

        var required = [];
        var optional = [];
        var ParamsRelatedType;

        var length = Types.length;
        var optionalIndex = 0;

        //types must be in this order:
        //[...][option[option[option...]]][params][...]
        var status = 0;

        for (var i = 0; i < length; i++) {
            var Type = this.Types[i];
            var paramType =
                typeof Type == "function" ?
                    ParamType.normal :
                    Type ? Type.__type__ || ParamType.normal : undefined;
                
            switch (paramType) {
                case ParamType.normal:
                case ParamType.delegate:
                    if (status == 0)
                        optionalIndex++;
                    else if (status < 3)
                        status = 3;
                    required.push(Type);
                    break;
                case ParamType.option:
                    if (status < 1)
                        status = 1;
                    if (status > 1)
                        throw new Error('"option" parameter position invalid.');
                    optional.push(Type);
                    break;
                case ParamType.params:
                    if (status < 2) {
                        status = 2;
                        ParamsRelatedType = Type.RelatedType;
                    }
                    else if (status > 2)
                        throw new Error('"params" parameter position invalid.');
                    else
                        throw new Error('"params" parameter can only appear once in an overload.');
                    break;
            }
        }

        this.exec = function (thisArg, args) {
            var result = {
                match: false,
                value: undefined
            };

            if (this.ThisType && !is_(thisArg, this.ThisType))
                return result;

            if (this.thisObject)
                thisArg = this.thisObject.value;

            var diff = args.length - required.length;

            if (
                diff < 0 ||
                (!ParamsRelatedType && diff > optional.length)
            ) return result;

            var destArgs = [];

            if (this.staticObject)
                destArgs.push(this.staticObject);

            // the first part of required.
            for (var i = 0; i < optionalIndex; i++) {
                var arg = args[i];
                var Type = required[i];

                if (!is_(arg, Type))
                    return result;

                if (Type.__type__ == ParamType.delegate)
                    arg = wrapDelegate(arg, Type);

                destArgs.push(arg);
            }

            // the optional part.
            var opLength = Math.min(diff, optional.length);
            for (var i = 0; i < optional.length; i++) {
                var arg =
                    i < opLength ?
                    args[i + optionalIndex] :
                    optional[i].defaultValue;

                var Type = optional[i].RelatedType;

                if (!is_(arg, Type))
                    return result;

                if (Type.__type__ == ParamType.delegate)
                    arg = wrapDelegate(arg, Type);

                destArgs.push(arg);
            }

            // the params part.
            var paEnd = diff + optionalIndex;
            var paLength = diff - opLength;
            var paStart = optionalIndex + opLength;

            if (paLength == 1 && is_(args[paStart], List(ParamsRelatedType)))
                destArgs.push(slice.call(args[paStart]));
            else if (ParamsRelatedType) {
                var paArray = [];
                for (var i = paStart; i < paEnd; i++) {
                    var arg = args[i];
                    var Type = ParamsRelatedType;

                    if (!is_(arg, Type))
                        return result;

                    if (Type.__type__ == ParamType.delegate)
                        arg = wrapDelegate(arg, Type);

                    paArray.push(arg);
                }
                destArgs.push(paArray);
            }

            // the second part of required.
            for (var i = optionalIndex; i < required.length; i++) {
                var arg = args[i + diff];
                var Type = required[i];

                if (!is_(arg, Type))
                    return result;

                if (Type.__type__ == ParamType.delegate)
                    arg = wrapDelegate(arg, Type);

                destArgs.push(arg);
            }

            var value = fn.apply(thisArg, destArgs);

            if (this.ReturnType && !is_(value, this.ReturnType))
                throw new Error("the function returned a value (" + value + ") doesn't match the type (" + getTypeName(this.ReturnType) + ") specified.");

            result.match = true;
            result.value = value;

            return result;
        };

        this.toString = function () {
            return fn.toString();
        };
    }

    function OverloadCollection(method) {
        var list = [];

        this.add = function (overload) {
            main:
            for (var i = 0; i < list.length; i++) {
                var item = list[i];

                if (item.ThisType != overload.ThisType)
                    continue;

                var TypesA = item.Types;
                var TypesB = overload.Types;

                if (TypesA.length != TypesB.length)
                    continue;

                for (var j = 0; j < TypesB.length; j++) {
                    if (!isTypeEqual(TypesA[j], TypesB[j]))
                        continue main;
                }

                list[i] = overload;
                warn("An overload is overriden.");
                return;
            }

            list.push(overload);
        };

        this.exec = function (thisArg, args) {
            var value;
            for (var i = 0; i < list.length; i++) {
                var result = list[i].exec(thisArg, args);
                if (result.match)
                    return result.value;
            }

            throw new Error("no overload " + (method.__name__ ? 'of method "' + method.__name__ + '" ' : "") + "matches arguments given.");
        };

        this.toString = function () {
            return list.join("\n-\n");
        };
    }

    /* EXTENDED TYPES */
    var IList = interface_("IList", {
        length: Integer
    });

    function PlainObject() { return {}; }
    PlainObject.__isInstance__ = function (object) {
        return !!object && typeof object == "object" && object.constructor == Object;
    };

    function List(Type) {
        return {
            __isInstance__: function (object) {
                if (!is_(object, Array)) return false;

                for (var i = 0; i < object.length; i++) {
                    var item = object[i];
                    if (!is_(item, Type))
                        return false;
                }

                return true;
            },
            __relatedType__: List,
            __name__: getTypeName(Type) + "[]"
        };
    }

    function Type() {
        return {
            __relatedInstance__: function () { }
        };
    }
    Type.__isInstance__ = function (object) {
        return isType(object);
    };

    function Integer(n) { return new Number(arguments.length > 0 ? Math.floor(n) : 0); }
    Integer.__isInstance__ = function (object) {
        return typeof object == "number" && object % 1 == 0;
    };

    global.PlainObject = PlainObject;
    global.IList = IList;
    global.List = _(Type, List);
    global.Type = Type;
    global.Integer = Integer;

    "Add __name__",
    function () {
        for (var i in global) {
            if (typeof global[i] == "function" && /[A-Z]/.test(i.charAt(0)) && !global[i].__name__)
                global[i].__name__ = i;
        }

        var list = "Number|String|Boolean|RegExp|Array|Function|Object".split("|");
        for (var i = 0; i < list.length; i++)
            global[list[i]].__name__ = list[i];
    }();

    global.is_ = _(nul_(Object), Type, is_).as_(Boolean);

    /* OTHER EXTENDED METHODS */

    global.for_ = _(nul_(IList), delegate_(Object, Integer, Integer, null), function for_(array, handler) {
        if (!array) return true;
        for (var i = 0; i < array.length;) {
            var result = handler(array[i], i, array.length);
            if (result === false)
                return false;
            else if (typeof result == "number")
                i += result;
            else
                i++;
        }
        return true;
    }).as_(Boolean);

    global.for_._(params_(IList), Function, function (arrays, handler) {
        var indexes = [];
        var items = [];

        var i;
        for (var i = 0; i < arrays.length; i++) {
            if (arrays[i].length == 0)
                return true;
            indexes[i] = 0;
            items[i] = arrays[i][0];
        }

        do {
            var result = handler.apply(null, items);

            if (result === false)
                return false;

            for (i = arrays.length - 1; i >= 0; i--) {
                indexes[i]++;
                if (indexes[i] < arrays[i].length) {
                    items[i] = arrays[i][indexes[i]];
                    break;
                }
                else {
                    indexes[i] = 0;
                    items[i] = arrays[i][0];
                }
            }
        }
        while (i >= 0);

        return true;
    }).as_(Boolean);

    global.forin_ = _(nul_(Object), delegate_(Object, String, null), function (object, handler) {
        if (!object) return true;
        for (var i in object)
            if (hasOwnProperty.call(object, i))
                if (handler(object[i], i) === false)
                    return false;
        return true;
    }).as_(Boolean);

    function enum_(name, items) {
        var count = items.length;

        if (count > 32)
            throw new Error("the length of enumeration list has exceeded the limit of 32.");

        function Enum(name, value) {
            this.toString = function () { return name; };
            this.valueOf = function () { return value; };
            this.test = _(Enum, function (value) { return !!(this & value); });
        }

        Enum.__isInstance__ = function (object) {
            return (
                object instanceof Enum ||
                is_(object, Integer) && object < 1 << count + 1
            );
        };

        Enum.__name__ = name;

        for (var i = 0; i < count; i++)
            var ele = Enum[items[i]] = new Enum(items[i], 1 << i);

        return Enum;
    }

    global.enum_ = _(String, List(String), enum_);
    global.enum_._(params_(String), function (items) {
        return enum_("", items);
    });

    /* VEJIS CLASS ENHANCING */

    var createMethod = function (name, Types, fn) {
        if (!is_(name, String) || name.length == 0)
            throw new Error('"name" must be a non-empty string.');

        var method;
        if ((method = this[name]) && method._)
            method._.apply(null, slice.call(arguments, 1)).bind_(this);
        else
            method = this[name] = _.apply(null, arguments).bind_(this);

        return method;
    };

    function class_(name, ClassBody) {
        var pri = {};

        var info = {
            inheritInfo: undefined,
            staticBody: undefined,
            ClassBody: ClassBody,
            Class: undefined
        };

        var relatedInstance;
        var theInterface;

        var Class = function () {
            var that = this;

            var ClassBodies = [];
            var inheritInfo = info;

            while (inheritInfo = inheritInfo.inheritInfo)
                ClassBodies.unshift(inheritInfo.ClassBody);

            var constructor, sup;

            this.var_ = _(params_(String), Type, function (names, Type) {
                for (var i = 0; i < names.length; i++)
                    this[names[i]] = undefined;
            });

            this._ = function (name, Types, fn) {
                if (is_(name, String)) {
                    if (name.length == 0)
                        throw new Error('"name" must be a non-empty string.');

                    var method;
                    if ((method = this[name]) && method._)
                        method._.apply(null, slice.call(arguments, 1)).bind_(this);
                    else
                        method = this[name] = _.apply(null, arguments).bind_(this);

                    return method;
                }

                if (!constructor)
                    constructor = _.apply(null, arguments).bind_(this);
                else
                    constructor._.apply(null, arguments).bind_(this);
            };

            for (var i = 0; i < ClassBodies.length; i++) {
                sup = constructor;
                constructor = null;
                var ins = ClassBodies[i].call(this, Class, pri, sup);
                if (ins) {
                    var type = typeof ins;
                    if (type == "function" || type == "object")
                        copy(ins, this, true);
                }
                if (!constructor)
                    constructor = _.call(null, function () { });
            }

            sup = constructor;
            constructor = null;
            var ins = ClassBody.call(this, Class, pri, sup);

            if (!constructor)
                constructor = _.call(null, function () { });

            delete this.var_;
            delete this._;
            constructor.apply(this, arguments);

            var o = this;

            if (ins) {
                var type = typeof ins;
                if (type == "function" || type == "object") {
                    copy(this, ins, false);
                    ins.constructor = Class;
                    ins.__relatedInstance__ = relatedInstance;
                    o = ins;
                }
            }

            if (theInterface) {
                if (!is_(o, theInterface))
                    throw new Error("some of the items defined in the interface are not implemented.");

                interfaceFormat(o, theInterface);
            }

            return ins;
        };

        Class.__name__ = name;
        Class.__classInfo__ = info;

        Class.prototype_ = _(PlainObject, function (object) {
            copy(object, Class.prototype, true);

            return Class;
        });

        Class.prototype_._(Function, function (builder) {
            Class.prototype._ = createMethod;
            builder.call(Class.prototype);
            delete Class.prototype._;

            return Class;
        });

        Class.static_ = _(PlainObject, function (body) {
            delete Class.static_;
            info.staticBody = body;

            copy(body, pri, true);

            return Class;
        });

        Class.static_._(Function, function (body) {
            delete Class.static_;
            info.staticBody = body;

            buildStatic(Class, pri, body, true);

            return Class;
        });

        Class.inherit_ = _(Function, function (BaseClass) {
            delete Class.inherit_;

            var Bridge = function () { this.constructor = Class; };
            Bridge.prototype = BaseClass.prototype;
            Class.prototype = new Bridge();

            var Related = function () { this.constructor = Class; };
            Related.prototype = Class.prototype;
            relatedInstance = new Related();
            
            var inheritInfo = BaseClass.__classInfo__;
            
            if (inheritInfo) {
                info.inheritInfo = inheritInfo;

                do {
                    var staticBody = inheritInfo.staticBody;
                    if (typeof staticBody == "function")
                        buildStatic(Class, pri, staticBody, false);
                    else
                        copy(staticBody, pri, false);
                }
                while (inheritInfo = inheritInfo.inheritInfo);
            }
            else {
                info.inheritInfo = {
                    ClassBody: BaseClass,
                    Class: BaseClass
                };
            }
            
            return Class;
        });
        
        Class.implement_ = _(Interface, function (Interface) {
            delete Class.inherit_;
            delete Class.implement_;
            theInterface = Interface;

            return Class;
        });

        info.Class = Class;

        return Class;
    }

    function buildStatic(pub, pri, staticBody, overwrite) {
        var o = {
            private_: _(PlainObject, function (priBody) {
                delete o.private_;
                copy(priBody, pri, overwrite);
            }),
            public_: _(PlainObject, function (pubBody) {
                delete o.public_;
                copy(pubBody, pub, overwrite);
            })
        };

        staticBody.call(o, pub, pri);
    }

    function interfaceFormat(ins, theInterface) {
        var list = theInterface.__list__;
        for (var i = 0; i < list.length; i++) {
            var item = list[i];
            var name = item.name;
            var Type = item.Type;
            var p = ins[name];
            if (Type.__type__ == ParamType.delegate) {
                p = _.apply(null, Type.__RelatedTypes__.concat(p));
                if (Type.__relatedThisObject__)
                    p.bind_(Type.__relatedThisObject__.value);
                else if (Type.__RelatedThisType__)
                    p.with_(Type.__RelatedThisType__);
                if (Type.__RelatedReturnType__)
                    p.as_(Type.__RelatedReturnType__);
                ins[name] = p;
            }
            else if (is_(Type, Interface))
                interfaceFormat(p, Type);
        }
    }

    global.class_ = _(opt_(String), Function, class_);

    function interface_(name, body) {
        var list = [];
        var hash = {};

        for (var i in body) {
            if (hasOwnProperty.call(body, i)) {
                var Type = body[i];
                list.push({
                    name: i,
                    Type: Type
                });
                hash[i] = true;
            }
        }

        var theInterface = new Interface();

        theInterface.__isInstance__ = function (object) {
            if (!object) return false;

            for (var i = 0; i < list.length; i++) {
                var item = list[i];
                if (!is_(object[item.name], item.Type))
                    return false;
            }

            return true;
        };
        theInterface.__name__ = name;
        theInterface.__list__ = list;
        theInterface.inherit_ = function (target) {
            /// <param name="target" type="Interface">the interface to inherit.</param>
            if (!is_(target, Interface))
                throw new Error("parameter target should be Interface.");

            var pList = target.__list__ || [];
            for (var i = 0; i < pList.length; i++) {
                var item = pList[i];
                if (!hasOwnProperty.call(hash, item.name)) {
                    list.push(item);
                    hash[item.name] = true;
                }
            }

            return theInterface;
        };

        return theInterface;
    }

    function Interface() { }

    global.Interface = Interface;
    global.interface_ = _(opt_(String), PlainObject, interface_);

    /* VEJIS MODULE SYSTEM */

    var moduleInvoker = new function () {
        var infos = createStringMap();

        this.addUse = addUse;

        function addUse(names, handler) {
            var remain = names.length;
            var modules = [];

            for (var i = 0; i < names.length; i++) (function (i) {
                var info = getInfo(names[i]);

                if (info.ready)
                    callback();
                else
                    info.callbacks.push(callback);

                function callback() {
                    modules[i] = info.module;
                    remain--;
                    if (remain == 0)
                        invoke();
                }
            })(i);

            function invoke() {
                try {
                    handler.apply(null, modules);
                }
                catch (e) {
                    var str = handler.toString();
                    var start = "in use_ handler, ";
                    if (e.message.indexOf(start) != 0)
                        e.message = "in use_ handler, " + e.message + "\n" + str.substr(0, 100) + (str.length > 100 ? "..." : "");
                    throw e;
                }
            }
        }

        this.addModule = function (name, parts, builder) {
            var info = getInfo(name);

            var module = info.module;

            if (info.created) {
                warn('module "' + name + '" already exists.');
                return;
            }

            info.created = true;

            if (builder)
                builder.call(module);

            if (parts.length > 0) {
                var start = name + "/";

                for (var i = 0; i < parts.length; i++) {
                    var sub = parts[i];
                    if (sub.indexOf(start) != 0)
                        parts[i] = start + sub;
                }

                addUse(parts, handler);
            }
            else
                handler();

            function handler() {
                info.ready = true;

                if (info.isRoot) {
                    delete module._;
                    delete module.delegate_;
                    delete module.enum_;
                    delete module.class_;
                    delete module.interface_;
                }

                var callbacks = info.callbacks;
                if (callbacks.length > 0) {
                    for (var i = 0; i < callbacks.length; i++)
                        callbacks[i]();
                    callbacks.length = 0;
                }
            }
        };

        this.getModule = function (name) {
            var info = infos(name);
            if (info && info.isRoot && info.ready)
                return info.module;
            else
                return undefined;
        };

        var createDelegate = function (name, Types, body) {
            if (!is_(name, String) || name.length == 0)
                throw new Error('"name" must be a non-empty string.');

            return this[name] = delegate_.apply(null, arguments);
        };

        var createEnum = _(String, List(String), function (name, eles) {
            return this[name] = enum_(name, eles);
        });

        var createClass = _(String, Function, function (name, ClassBody) {
            return this[name] = class_(name, ClassBody);
        });

        var createInterface = _(String, PlainObject, function (name, body) {
            return this[name] = interface_(name, body);
        });

        function getInfo(name) {
            var info = infos(name);

            var baseName = name.match(/^[^\/]+/)[0];
            var isRoot = baseName == name;

            if (!info) {
                info = {
                    module: isRoot ? {
                        _: createMethod,
                        delegate_: createDelegate,
                        enum_: createEnum,
                        class_: createClass,
                        interface_: createInterface
                    } : getInfo(baseName).module,
                    isRoot: isRoot,
                    ready: false,
                    created: false,
                    callbacks: []
                };
                infos(name, info);
            }

            return info;
        }
    }();

    global.module_ = _(String, Function, function (name, builder) {
        moduleInvoker.addModule(name, [], builder);
    });

    global.module_._(String, List(String), opt_(nul_(Function)), function (name, parts, builder) {
        moduleInvoker.addModule(name, parts, builder);
    });

    global.use_ = _(params_(String), Function, function (names, handler) {
        moduleInvoker.addUse(names, handler);
    });

    global.import_ = _(String, function (name) {
        var module = moduleInvoker.getModule(name);
        if (module)
            return module;
        else {
            warn('module "' + name + '" has not been loaded.');
            return undefined;
        }
    });

    var requiredFiles = {};

    global.require_ = _(params_(String), require_);
    global.require_._(String, List(String), function (baseDir, srcs) {
        for (var i = 0; i < srcs.length; i++)
            srcs[i] = baseDir + srcs[i];
        return require_(srcs);
    });

    function require_(srcs) {
        var head = document.getElementsByTagName("head")[0];
        for (var i = 0; i < srcs.length; i++) {
            var src = srcs[i];
            if (hasOwnProperty.call(requiredFiles, src))
                return;

            requiredFiles[src] = true;
            var script = document.createElement("script");
            script.type = "text/javascript";
            script.async = "async";
            script.src = src;
            head.insertBefore(script, head.firstChild);
        }
    }

}();
