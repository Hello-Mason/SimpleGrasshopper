﻿using SimpleGrasshopper.Attributes;
using SimpleGrasshopper.Util;

namespace SimpleGrasshopper.Data;

internal readonly struct ParameterParam(ParameterInfo info, int index, int methodIndex)
{
    public int MethodParamIndex => methodIndex;

    public TypeParam Param { get; } = new(info.ParameterType, index);

    public ParameterInfo ParamInfo => info;

    public GH_ParamAccess Access => Param.Access;

    public void GetNames(string defaultName, string defaultNickName, out string name, out string nickName, out string description)
    {
        var attr = ParamInfo.GetCustomAttribute<DocObjAttribute>();
        defaultName = ParamInfo.Name ?? defaultName;
        defaultNickName = ParamInfo.Name ?? defaultNickName;

        name = attr?.Name ?? defaultName;
        nickName = attr?.NickName ?? defaultNickName;
        description = attr?.Description ?? defaultName;
    }

    public IGH_Param CreateParam()
    {
        var proxy = Instances.ComponentServer.EmitObjectProxy(
            ParamInfo.GetCustomAttribute<ParamAttribute>()?.Guid ?? Param.ComponentGuid);

        if (proxy.CreateInstance() is not IGH_Param param)
        {
            throw new ArgumentException("The guid is not valid for creating a IGH_Param!");
        }

        SetOptional(ParamInfo, param, Access);
        Utils.SetSpecial(param, Param.RawInnerType,
            ParamInfo.GetCustomAttribute<AngleAttribute>() != null,
            ParamInfo.GetCustomAttribute<HiddenAttribute>() != null);

        return param;

        static void SetOptional(ParameterInfo info, IGH_Param param, GH_ParamAccess access)
        {
            if (access == GH_ParamAccess.item && info.DefaultValue != null)
            {
                SetPersistentData(ref param, info.DefaultValue);
            }
            else if (info.HasDefaultValue)
            {
                param.Optional = true;
            }

            static void SetPersistentData(ref IGH_Param param, object data)
            {
                var persistType = typeof(GH_PersistentParam<>);
                if (param.GetType().IsGeneralType(persistType) is not Type persistParam) return;

                var method = persistType.MakeGenericType(persistParam).GetRuntimeMethod("SetPersistentData", [typeof(object[])]);

                if (method == null) return;
                method.Invoke(param, [new object[] { data }]);
            }
        }
    }

    public bool GetValue(IGH_DataAccess DA, out object value, IGH_Param param)
    {
        if (!Param.GetValue(DA, out value))
        {
            return false;
        }

        //Modify range
        var messages = ParamInfo.GetCustomAttribute<RangeAttribute>() is RangeAttribute range
            ? Utils.ModifyRange(ref value, range, Access) : [];

        Utils.ModifyAngle(ref value, param);

        param.AddRuntimeMessages(messages);

        return true;
    }

    public bool SetValue(IGH_DataAccess DA, object? value) => Param.SetValue(DA, value);
}
