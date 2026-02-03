#include "pch-cpp.hpp"





template <typename R>
struct VirtualFuncInvoker0
{
	typedef R (*Func)(void*, const RuntimeMethod*);

	static inline R Invoke (Il2CppMethodSlot slot, RuntimeObject* obj)
	{
		const VirtualInvokeData& invokeData = il2cpp_codegen_get_virtual_invoke_data(slot, obj);
		return ((Func)invokeData.methodPtr)(obj, invokeData.method);
	}
};
struct InterfaceActionInvoker0
{
	typedef void (*Action)(void*, const RuntimeMethod*);

	static inline void Invoke (Il2CppMethodSlot slot, RuntimeClass* declaringInterface, RuntimeObject* obj)
	{
		const VirtualInvokeData& invokeData = il2cpp_codegen_get_interface_invoke_data(slot, obj, declaringInterface);
		((Action)invokeData.methodPtr)(obj, invokeData.method);
	}
};
template <typename R>
struct InterfaceFuncInvoker0
{
	typedef R (*Func)(void*, const RuntimeMethod*);

	static inline R Invoke (Il2CppMethodSlot slot, RuntimeClass* declaringInterface, RuntimeObject* obj)
	{
		const VirtualInvokeData& invokeData = il2cpp_codegen_get_interface_invoke_data(slot, obj, declaringInterface);
		return ((Func)invokeData.methodPtr)(obj, invokeData.method);
	}
};
template <typename R, typename T1>
struct InterfaceFuncInvoker1
{
	typedef R (*Func)(void*, T1, const RuntimeMethod*);

	static inline R Invoke (Il2CppMethodSlot slot, RuntimeClass* declaringInterface, RuntimeObject* obj, T1 p1)
	{
		const VirtualInvokeData& invokeData = il2cpp_codegen_get_interface_invoke_data(slot, obj, declaringInterface);
		return ((Func)invokeData.methodPtr)(obj, p1, invokeData.method);
	}
};

struct ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031;
struct CharU5BU5D_t799905CF001DD5F13F7DBB310181FC4D8B7D0AAB;
struct IDictionary_t6D03155AF1FA9083817AA5B6AD7DEEACC26AB220;
struct IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5;
struct IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA;
struct IList_t1C522956D79B7DC92B5B01053DF1AC058C8B598D;
struct String_t;
struct StringBuilder_t;
struct Serializer_t080A530C38A3C9BBE2EB8CB81F36149BCFE25635;

IL2CPP_EXTERN_C RuntimeClass* Convert_t7097FF336D592F7C06D88A98349A44646F91EFFC_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Decimal_tDA6C877282B2D789CF97C0949661CC11D643969F_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IDictionary_t6D03155AF1FA9083817AA5B6AD7DEEACC26AB220_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IEnumerable_t6331596D5DD37C462B1B8D49CF6B319B00AB7131_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IList_t1C522956D79B7DC92B5B01053DF1AC058C8B598D_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Serializer_t080A530C38A3C9BBE2EB8CB81F36149BCFE25635_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* StringBuilder_t_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C String_t* _stringLiteral2C3D4826D5236B3C9A914C5CE2E3D8CEA48AC7CE;
IL2CPP_EXTERN_C String_t* _stringLiteral5962E944D7340CE47999BF097B4AFD70C1501FB9;
IL2CPP_EXTERN_C String_t* _stringLiteral5BEFD8CC60A79699B5BB00E37BAC5B62D371E174;
IL2CPP_EXTERN_C String_t* _stringLiteral77D38C0623F92B292B925F6E72CF5CF99A20D4EB;
IL2CPP_EXTERN_C String_t* _stringLiteral785F17F45C331C415D0A7458E6AAC36966399C51;
IL2CPP_EXTERN_C String_t* _stringLiteral7F3238CD8C342B06FB9AB185C610175C84625462;
IL2CPP_EXTERN_C String_t* _stringLiteral848E5ED630B3142F565DD995C6E8D30187ED33CD;
IL2CPP_EXTERN_C String_t* _stringLiteralA7C3FCA8C63E127B542B38A5CA5E3FEEDDD1B122;
IL2CPP_EXTERN_C String_t* _stringLiteralB78F235D4291950A7D101307609C259F3E1F033F;
IL2CPP_EXTERN_C String_t* _stringLiteralB7C45DD316C68ABF3429C20058C2981C652192F2;
IL2CPP_EXTERN_C String_t* _stringLiteralDA666908BB15F4E1D2649752EC5DCBD0D5C64699;
IL2CPP_EXTERN_C String_t* _stringLiteralE727BF366E3CC855B808D806440542BF7152AF19;
IL2CPP_EXTERN_C String_t* _stringLiteralF18840F490E42D3CE48CDCBF47229C1C240F8ABE;

struct CharU5BU5D_t799905CF001DD5F13F7DBB310181FC4D8B7D0AAB;

IL2CPP_EXTERN_C_BEGIN
IL2CPP_EXTERN_C_END

#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
struct U3CPrivateImplementationDetailsU3E_t0F5473E849A5A5185A9F4C5246F0C32816C49FCA  : public RuntimeObject
{
};
struct String_t  : public RuntimeObject
{
	int32_t ____stringLength;
	Il2CppChar ____firstChar;
};
struct StringBuilder_t  : public RuntimeObject
{
	CharU5BU5D_t799905CF001DD5F13F7DBB310181FC4D8B7D0AAB* ___m_ChunkChars;
	StringBuilder_t* ___m_ChunkPrevious;
	int32_t ___m_ChunkLength;
	int32_t ___m_ChunkOffset;
	int32_t ___m_MaxCapacity;
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F  : public RuntimeObject
{
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F_marshaled_pinvoke
{
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F_marshaled_com
{
};
struct Serializer_t080A530C38A3C9BBE2EB8CB81F36149BCFE25635  : public RuntimeObject
{
	StringBuilder_t* ___builder;
};
struct Boolean_t09A6377A54BE2F9E6985A8149F19234FD7DDFE22 
{
	bool ___m_value;
};
struct Byte_t94D9231AC217BE4D2E004C4CD32DF6D099EA41A3 
{
	uint8_t ___m_value;
};
struct Char_t521A6F19B456D956AF452D926C32709DC03D6B17 
{
	Il2CppChar ___m_value;
};
struct Decimal_tDA6C877282B2D789CF97C0949661CC11D643969F 
{
	union
	{
		#pragma pack(push, tp, 1)
		struct
		{
			int32_t ___flags;
		};
		#pragma pack(pop, tp)
		struct
		{
			int32_t ___flags_forAlignmentOnly;
		};
		#pragma pack(push, tp, 1)
		struct
		{
			char ___hi_OffsetPadding[4];
			int32_t ___hi;
		};
		#pragma pack(pop, tp)
		struct
		{
			char ___hi_OffsetPadding_forAlignmentOnly[4];
			int32_t ___hi_forAlignmentOnly;
		};
		#pragma pack(push, tp, 1)
		struct
		{
			char ___lo_OffsetPadding[8];
			int32_t ___lo;
		};
		#pragma pack(pop, tp)
		struct
		{
			char ___lo_OffsetPadding_forAlignmentOnly[8];
			int32_t ___lo_forAlignmentOnly;
		};
		#pragma pack(push, tp, 1)
		struct
		{
			char ___mid_OffsetPadding[12];
			int32_t ___mid;
		};
		#pragma pack(pop, tp)
		struct
		{
			char ___mid_OffsetPadding_forAlignmentOnly[12];
			int32_t ___mid_forAlignmentOnly;
		};
		#pragma pack(push, tp, 1)
		struct
		{
			char ___ulomidLE_OffsetPadding[8];
			uint64_t ___ulomidLE;
		};
		#pragma pack(pop, tp)
		struct
		{
			char ___ulomidLE_OffsetPadding_forAlignmentOnly[8];
			uint64_t ___ulomidLE_forAlignmentOnly;
		};
	};
};
struct Double_tE150EF3D1D43DEE85D533810AB4C742307EEDE5F 
{
	double ___m_value;
};
struct Enum_t2A1A94B24E3B776EEF4E5E485E290BB9D4D072E2  : public ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F
{
};
struct Enum_t2A1A94B24E3B776EEF4E5E485E290BB9D4D072E2_marshaled_pinvoke
{
};
struct Enum_t2A1A94B24E3B776EEF4E5E485E290BB9D4D072E2_marshaled_com
{
};
struct Int16_tB8EF286A9C33492FA6E6D6E67320BE93E794A175 
{
	int16_t ___m_value;
};
struct Int32_t680FF22E76F6EFAD4375103CBBFFA0421349384C 
{
	int32_t ___m_value;
};
struct Int64_t092CFB123BE63C28ACDAF65C68F21A526050DBA3 
{
	int64_t ___m_value;
};
struct SByte_tFEFFEF5D2FEBF5207950AE6FAC150FC53B668DB5 
{
	int8_t ___m_value;
};
struct Single_t4530F2FF86FCB0DC29F35385CA1BD21BE294761C 
{
	float ___m_value;
};
struct UInt16_tF4C148C876015C212FD72652D0B6ED8CC247A455 
{
	uint16_t ___m_value;
};
struct UInt32_t1833D51FFA667B18A5AA4B8D34DE284F8495D29B 
{
	uint32_t ___m_value;
};
struct UInt64_t8F12534CC8FC4B5860F2A2CD1EE79D322E7A41AF 
{
	uint64_t ___m_value;
};
struct Void_t4861ACF8F4594C3437BB48B6E56783494B843915 
{
	union
	{
		struct
		{
		};
		uint8_t Void_t4861ACF8F4594C3437BB48B6E56783494B843915__padding[1];
	};
};
#pragma pack(push, tp, 1)
struct __StaticArrayInitTypeSizeU3D16356_t9899CD9CB1BE26AAE599403593AA228FFB2D8D8E 
{
	union
	{
		struct
		{
			union
			{
			};
		};
		uint8_t __StaticArrayInitTypeSizeU3D16356_t9899CD9CB1BE26AAE599403593AA228FFB2D8D8E__padding[16356];
	};
};
#pragma pack(pop, tp)
#pragma pack(push, tp, 1)
struct __StaticArrayInitTypeSizeU3D9125_t9F20D86ADE829EDE1CF00B8374CF423806D56B20 
{
	union
	{
		struct
		{
			union
			{
			};
		};
		uint8_t __StaticArrayInitTypeSizeU3D9125_t9F20D86ADE829EDE1CF00B8374CF423806D56B20__padding[9125];
	};
};
#pragma pack(pop, tp)
struct TOKEN_tE9BCDD18E51944946561C64814AAC2886E9852A0 
{
	int32_t ___value__;
};
struct U3CPrivateImplementationDetailsU3E_t0F5473E849A5A5185A9F4C5246F0C32816C49FCA_StaticFields
{
	__StaticArrayInitTypeSizeU3D9125_t9F20D86ADE829EDE1CF00B8374CF423806D56B20 ___047ABF20959E8FC1D2AFCE8ADF7C1EECEFC1710522F495FA2F66F63AE0FC9E38;
	__StaticArrayInitTypeSizeU3D16356_t9899CD9CB1BE26AAE599403593AA228FFB2D8D8E ___59796629E4E9DC088442DF5D35B1219AEEF5839046604582ACAC0E1A84C30E41;
};
struct String_t_StaticFields
{
	String_t* ___Empty;
};
struct Boolean_t09A6377A54BE2F9E6985A8149F19234FD7DDFE22_StaticFields
{
	String_t* ___TrueString;
	String_t* ___FalseString;
};
struct Char_t521A6F19B456D956AF452D926C32709DC03D6B17_StaticFields
{
	ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ___s_categoryForLatin1;
};
struct Decimal_tDA6C877282B2D789CF97C0949661CC11D643969F_StaticFields
{
	Decimal_tDA6C877282B2D789CF97C0949661CC11D643969F ___Zero;
	Decimal_tDA6C877282B2D789CF97C0949661CC11D643969F ___One;
	Decimal_tDA6C877282B2D789CF97C0949661CC11D643969F ___MinusOne;
	Decimal_tDA6C877282B2D789CF97C0949661CC11D643969F ___MaxValue;
	Decimal_tDA6C877282B2D789CF97C0949661CC11D643969F ___MinValue;
};
#ifdef __clang__
#pragma clang diagnostic pop
#endif
struct CharU5BU5D_t799905CF001DD5F13F7DBB310181FC4D8B7D0AAB  : public RuntimeArray
{
	ALIGN_FIELD (8) Il2CppChar m_Items[1];

	inline Il2CppChar GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline Il2CppChar* GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, Il2CppChar value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
	}
	inline Il2CppChar GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline Il2CppChar* GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, Il2CppChar value)
	{
		m_Items[index] = value;
	}
};



IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2 (RuntimeObject* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void StringBuilder__ctor_m1D99713357DE05DAFA296633639DB55F8C30587D (StringBuilder_t* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Serializer__ctor_m1AC65B7201AF35020621757EDA9CF2F517123614 (Serializer_t080A530C38A3C9BBE2EB8CB81F36149BCFE25635* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Serializer_SerializeValue_m952078ABBCE9FDAF9FC44028879BBF5FC10C1030 (Serializer_t080A530C38A3C9BBE2EB8CB81F36149BCFE25635* __this, RuntimeObject* ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR StringBuilder_t* StringBuilder_Append_m08904D74E0C78E5F36DCD9C9303BDD07886D9F7D (StringBuilder_t* __this, String_t* ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Serializer_SerializeString_mB658D7F56B6D171FE905FE62A98E59DAC6050BDB (Serializer_t080A530C38A3C9BBE2EB8CB81F36149BCFE25635* __this, String_t* ___0_str, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Serializer_SerializeArray_mD635B32B9406AF9608B71640F32C16AB828138BB (Serializer_t080A530C38A3C9BBE2EB8CB81F36149BCFE25635* __this, RuntimeObject* ___0_anArray, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Serializer_SerializeObject_mEE17BC69DDCF30AD22C910401562491ADABE5426 (Serializer_t080A530C38A3C9BBE2EB8CB81F36149BCFE25635* __this, RuntimeObject* ___0_obj, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* String_CreateString_mAA0705B41B390BDB42F67894B9B67C956814C71B (String_t* __this, Il2CppChar ___0_c, int32_t ___1_count, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Serializer_SerializeOther_m5A394D1B6C91B809DFBF69B4F3B9FAE531853841 (Serializer_t080A530C38A3C9BBE2EB8CB81F36149BCFE25635* __this, RuntimeObject* ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR StringBuilder_t* StringBuilder_Append_m71228B30F05724CD2CD96D9611DCD61BFB96A6E1 (StringBuilder_t* __this, Il2CppChar ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR CharU5BU5D_t799905CF001DD5F13F7DBB310181FC4D8B7D0AAB* String_ToCharArray_m0699A92AA3E744229EF29CB9D943C47DF4FE5B46 (String_t* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t Convert_ToInt32_mDBBE9318A7CCE1560974CE93F5BFED9931CF0052 (Il2CppChar ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* Int32_ToString_m967AECC237535C552A97A80C7875E31B98496CA9 (int32_t* __this, String_t* ___0_format, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* Single_ToString_m3F2C4433B6ADFA5ED8E3F14ED19CD23014E5179D (float* __this, String_t* ___0_format, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR StringBuilder_t* StringBuilder_Append_m3A7D629DAA5E0E36B8A617A911E34F79AF84AE63 (StringBuilder_t* __this, RuntimeObject* ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Convert_ToDouble_m86FF4F837721833186E883102C056A35F0860EB0 (RuntimeObject* ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* Double_ToString_m70EC76E1DAD7E8B5B47AF9292189BF3711B24B75 (double* __this, String_t* ___0_format, const RuntimeMethod* method) ;
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
// Method Definition Index: 79943
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Serializer__ctor_m1AC65B7201AF35020621757EDA9CF2F517123614 (Serializer_t080A530C38A3C9BBE2EB8CB81F36149BCFE25635* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&StringBuilder_t_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:408>
		Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2(__this, NULL);
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:409>
		StringBuilder_t* L_0 = (StringBuilder_t*)il2cpp_codegen_object_new(StringBuilder_t_il2cpp_TypeInfo_var);
		StringBuilder__ctor_m1D99713357DE05DAFA296633639DB55F8C30587D(L_0, NULL);
		__this->___builder = L_0;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___builder), (void*)L_0);
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:410>
		return;
	}
}
// Method Definition Index: 79944
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* Serializer_Serialize_m70E06BB55F5615FF0EDE474912A8C2FEDBFBBE10 (RuntimeObject* ___0_obj, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Serializer_t080A530C38A3C9BBE2EB8CB81F36149BCFE25635_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:413>
		Serializer_t080A530C38A3C9BBE2EB8CB81F36149BCFE25635* L_0 = (Serializer_t080A530C38A3C9BBE2EB8CB81F36149BCFE25635*)il2cpp_codegen_object_new(Serializer_t080A530C38A3C9BBE2EB8CB81F36149BCFE25635_il2cpp_TypeInfo_var);
		Serializer__ctor_m1AC65B7201AF35020621757EDA9CF2F517123614(L_0, NULL);
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:415>
		Serializer_t080A530C38A3C9BBE2EB8CB81F36149BCFE25635* L_1 = L_0;
		RuntimeObject* L_2 = ___0_obj;
		NullCheck(L_1);
		Serializer_SerializeValue_m952078ABBCE9FDAF9FC44028879BBF5FC10C1030(L_1, L_2, NULL);
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:417>
		NullCheck(L_1);
		StringBuilder_t* L_3 = L_1->___builder;
		NullCheck(L_3);
		String_t* L_4;
		L_4 = VirtualFuncInvoker0< String_t* >::Invoke(3, L_3);
		return L_4;
	}
}
// Method Definition Index: 79945
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Serializer_SerializeValue_m952078ABBCE9FDAF9FC44028879BBF5FC10C1030 (Serializer_t080A530C38A3C9BBE2EB8CB81F36149BCFE25635* __this, RuntimeObject* ___0_value, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IDictionary_t6D03155AF1FA9083817AA5B6AD7DEEACC26AB220_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IList_t1C522956D79B7DC92B5B01053DF1AC058C8B598D_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral5BEFD8CC60A79699B5BB00E37BAC5B62D371E174);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral77D38C0623F92B292B925F6E72CF5CF99A20D4EB);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralB7C45DD316C68ABF3429C20058C2981C652192F2);
		s_Il2CppMethodInitialized = true;
	}
	RuntimeObject* V_0 = NULL;
	RuntimeObject* V_1 = NULL;
	String_t* V_2 = NULL;
	StringBuilder_t* G_B7_0 = NULL;
	StringBuilder_t* G_B6_0 = NULL;
	String_t* G_B8_0 = NULL;
	StringBuilder_t* G_B8_1 = NULL;
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:425>
		RuntimeObject* L_0 = ___0_value;
		if (L_0)
		{
			goto IL_0015;
		}
	}
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:426>
		StringBuilder_t* L_1 = __this->___builder;
		NullCheck(L_1);
		StringBuilder_t* L_2;
		L_2 = StringBuilder_Append_m08904D74E0C78E5F36DCD9C9303BDD07886D9F7D(L_1, _stringLiteral5BEFD8CC60A79699B5BB00E37BAC5B62D371E174, NULL);
		return;
	}

IL_0015:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:427>
		RuntimeObject* L_3 = ___0_value;
		String_t* L_4 = ((String_t*)IsInstSealed((RuntimeObject*)L_3, il2cpp_defaults.string_class));
		V_2 = L_4;
		if (!L_4)
		{
			goto IL_0027;
		}
	}
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:428>
		String_t* L_5 = V_2;
		Serializer_SerializeString_mB658D7F56B6D171FE905FE62A98E59DAC6050BDB(__this, L_5, NULL);
		return;
	}

IL_0027:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:429>
		RuntimeObject* L_6 = ___0_value;
		if (!((RuntimeObject*)IsInstSealed((RuntimeObject*)L_6, il2cpp_defaults.boolean_class)))
		{
			goto IL_0050;
		}
	}
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:430>
		StringBuilder_t* L_7 = __this->___builder;
		RuntimeObject* L_8 = ___0_value;
		if (((*(bool*)UnBox(L_8, il2cpp_defaults.boolean_class))))
		{
			G_B7_0 = L_7;
			goto IL_0044;
		}
		G_B6_0 = L_7;
	}
	{
		G_B8_0 = _stringLiteral77D38C0623F92B292B925F6E72CF5CF99A20D4EB;
		G_B8_1 = G_B6_0;
		goto IL_0049;
	}

IL_0044:
	{
		G_B8_0 = _stringLiteralB7C45DD316C68ABF3429C20058C2981C652192F2;
		G_B8_1 = G_B7_0;
	}

IL_0049:
	{
		NullCheck(G_B8_1);
		StringBuilder_t* L_9;
		L_9 = StringBuilder_Append_m08904D74E0C78E5F36DCD9C9303BDD07886D9F7D(G_B8_1, G_B8_0, NULL);
		return;
	}

IL_0050:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:431>
		RuntimeObject* L_10 = ___0_value;
		RuntimeObject* L_11 = ((RuntimeObject*)IsInst((RuntimeObject*)L_10, IList_t1C522956D79B7DC92B5B01053DF1AC058C8B598D_il2cpp_TypeInfo_var));
		V_0 = L_11;
		if (!L_11)
		{
			goto IL_0062;
		}
	}
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:432>
		RuntimeObject* L_12 = V_0;
		Serializer_SerializeArray_mD635B32B9406AF9608B71640F32C16AB828138BB(__this, L_12, NULL);
		return;
	}

IL_0062:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:433>
		RuntimeObject* L_13 = ___0_value;
		RuntimeObject* L_14 = ((RuntimeObject*)IsInst((RuntimeObject*)L_13, IDictionary_t6D03155AF1FA9083817AA5B6AD7DEEACC26AB220_il2cpp_TypeInfo_var));
		V_1 = L_14;
		if (!L_14)
		{
			goto IL_0074;
		}
	}
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:434>
		RuntimeObject* L_15 = V_1;
		Serializer_SerializeObject_mEE17BC69DDCF30AD22C910401562491ADABE5426(__this, L_15, NULL);
		return;
	}

IL_0074:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:435>
		RuntimeObject* L_16 = ___0_value;
		if (!((RuntimeObject*)IsInstSealed((RuntimeObject*)L_16, il2cpp_defaults.char_class)))
		{
			goto IL_008f;
		}
	}
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:436>
		RuntimeObject* L_17 = ___0_value;
		String_t* L_18;
		L_18 = String_CreateString_mAA0705B41B390BDB42F67894B9B67C956814C71B(NULL, ((*(Il2CppChar*)UnBox(L_17, il2cpp_defaults.char_class))), 1, NULL);
		Serializer_SerializeString_mB658D7F56B6D171FE905FE62A98E59DAC6050BDB(__this, L_18, NULL);
		return;
	}

IL_008f:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:438>
		RuntimeObject* L_19 = ___0_value;
		Serializer_SerializeOther_m5A394D1B6C91B809DFBF69B4F3B9FAE531853841(__this, L_19, NULL);
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:440>
		return;
	}
}
// Method Definition Index: 79946
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Serializer_SerializeObject_mEE17BC69DDCF30AD22C910401562491ADABE5426 (Serializer_t080A530C38A3C9BBE2EB8CB81F36149BCFE25635* __this, RuntimeObject* ___0_obj, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IDictionary_t6D03155AF1FA9083817AA5B6AD7DEEACC26AB220_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IEnumerable_t6331596D5DD37C462B1B8D49CF6B319B00AB7131_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	bool V_0 = false;
	RuntimeObject* V_1 = NULL;
	RuntimeObject* V_2 = NULL;
	RuntimeObject* V_3 = NULL;
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:443>
		V_0 = (bool)1;
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:445>
		StringBuilder_t* L_0 = __this->___builder;
		NullCheck(L_0);
		StringBuilder_t* L_1;
		L_1 = StringBuilder_Append_m71228B30F05724CD2CD96D9611DCD61BFB96A6E1(L_0, ((int32_t)123), NULL);
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:447>
		RuntimeObject* L_2 = ___0_obj;
		NullCheck(L_2);
		RuntimeObject* L_3;
		L_3 = InterfaceFuncInvoker0< RuntimeObject* >::Invoke(2, IDictionary_t6D03155AF1FA9083817AA5B6AD7DEEACC26AB220_il2cpp_TypeInfo_var, L_2);
		NullCheck(L_3);
		RuntimeObject* L_4;
		L_4 = InterfaceFuncInvoker0< RuntimeObject* >::Invoke(0, IEnumerable_t6331596D5DD37C462B1B8D49CF6B319B00AB7131_il2cpp_TypeInfo_var, L_3);
		V_1 = L_4;
	}
	{
		auto __finallyBlock = il2cpp::utils::Finally([&]
		{

FINALLY_0069:
			{
				{
					RuntimeObject* L_5 = V_1;
					V_3 = ((RuntimeObject*)IsInst((RuntimeObject*)L_5, IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var));
					RuntimeObject* L_6 = V_3;
					if (!L_6)
					{
						goto IL_0079;
					}
				}
				{
					RuntimeObject* L_7 = V_3;
					NullCheck(L_7);
					InterfaceActionInvoker0::Invoke(0, IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var, L_7);
				}

IL_0079:
				{
					return;
				}
			}
		});
		try
		{
			{
				goto IL_005f_1;
			}

IL_001e_1:
			{
				//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:447>
				RuntimeObject* L_8 = V_1;
				NullCheck(L_8);
				RuntimeObject* L_9;
				L_9 = InterfaceFuncInvoker0< RuntimeObject* >::Invoke(1, IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA_il2cpp_TypeInfo_var, L_8);
				V_2 = L_9;
				//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:448>
				bool L_10 = V_0;
				if (L_10)
				{
					goto IL_0036_1;
				}
			}
			{
				//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:449>
				StringBuilder_t* L_11 = __this->___builder;
				NullCheck(L_11);
				StringBuilder_t* L_12;
				L_12 = StringBuilder_Append_m71228B30F05724CD2CD96D9611DCD61BFB96A6E1(L_11, ((int32_t)44), NULL);
			}

IL_0036_1:
			{
				//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:452>
				RuntimeObject* L_13 = V_2;
				NullCheck(L_13);
				String_t* L_14;
				L_14 = VirtualFuncInvoker0< String_t* >::Invoke(3, L_13);
				Serializer_SerializeString_mB658D7F56B6D171FE905FE62A98E59DAC6050BDB(__this, L_14, NULL);
				//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:453>
				StringBuilder_t* L_15 = __this->___builder;
				NullCheck(L_15);
				StringBuilder_t* L_16;
				L_16 = StringBuilder_Append_m71228B30F05724CD2CD96D9611DCD61BFB96A6E1(L_15, ((int32_t)58), NULL);
				//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:455>
				RuntimeObject* L_17 = ___0_obj;
				RuntimeObject* L_18 = V_2;
				NullCheck(L_17);
				RuntimeObject* L_19;
				L_19 = InterfaceFuncInvoker1< RuntimeObject*, RuntimeObject* >::Invoke(0, IDictionary_t6D03155AF1FA9083817AA5B6AD7DEEACC26AB220_il2cpp_TypeInfo_var, L_17, L_18);
				Serializer_SerializeValue_m952078ABBCE9FDAF9FC44028879BBF5FC10C1030(__this, L_19, NULL);
				//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:457>
				V_0 = (bool)0;
			}

IL_005f_1:
			{
				//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:447>
				RuntimeObject* L_20 = V_1;
				NullCheck(L_20);
				bool L_21;
				L_21 = InterfaceFuncInvoker0< bool >::Invoke(0, IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA_il2cpp_TypeInfo_var, L_20);
				if (L_21)
				{
					goto IL_001e_1;
				}
			}
			{
				goto IL_007a;
			}
		}
		catch(Il2CppExceptionWrapper& e)
		{
			__finallyBlock.StoreException(e.ex);
		}
	}

IL_007a:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:460>
		StringBuilder_t* L_22 = __this->___builder;
		NullCheck(L_22);
		StringBuilder_t* L_23;
		L_23 = StringBuilder_Append_m71228B30F05724CD2CD96D9611DCD61BFB96A6E1(L_22, ((int32_t)125), NULL);
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:461>
		return;
	}
}
// Method Definition Index: 79947
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Serializer_SerializeArray_mD635B32B9406AF9608B71640F32C16AB828138BB (Serializer_t080A530C38A3C9BBE2EB8CB81F36149BCFE25635* __this, RuntimeObject* ___0_anArray, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IEnumerable_t6331596D5DD37C462B1B8D49CF6B319B00AB7131_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	bool V_0 = false;
	RuntimeObject* V_1 = NULL;
	RuntimeObject* V_2 = NULL;
	RuntimeObject* V_3 = NULL;
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:464>
		StringBuilder_t* L_0 = __this->___builder;
		NullCheck(L_0);
		StringBuilder_t* L_1;
		L_1 = StringBuilder_Append_m71228B30F05724CD2CD96D9611DCD61BFB96A6E1(L_0, ((int32_t)91), NULL);
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:466>
		V_0 = (bool)1;
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:468>
		RuntimeObject* L_2 = ___0_anArray;
		NullCheck(L_2);
		RuntimeObject* L_3;
		L_3 = InterfaceFuncInvoker0< RuntimeObject* >::Invoke(0, IEnumerable_t6331596D5DD37C462B1B8D49CF6B319B00AB7131_il2cpp_TypeInfo_var, L_2);
		V_1 = L_3;
	}
	{
		auto __finallyBlock = il2cpp::utils::Finally([&]
		{

FINALLY_0044:
			{
				{
					RuntimeObject* L_4 = V_1;
					V_3 = ((RuntimeObject*)IsInst((RuntimeObject*)L_4, IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var));
					RuntimeObject* L_5 = V_3;
					if (!L_5)
					{
						goto IL_0054;
					}
				}
				{
					RuntimeObject* L_6 = V_3;
					NullCheck(L_6);
					InterfaceActionInvoker0::Invoke(0, IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var, L_6);
				}

IL_0054:
				{
					return;
				}
			}
		});
		try
		{
			{
				goto IL_003a_1;
			}

IL_0019_1:
			{
				//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:468>
				RuntimeObject* L_7 = V_1;
				NullCheck(L_7);
				RuntimeObject* L_8;
				L_8 = InterfaceFuncInvoker0< RuntimeObject* >::Invoke(1, IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA_il2cpp_TypeInfo_var, L_7);
				V_2 = L_8;
				//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:469>
				bool L_9 = V_0;
				if (L_9)
				{
					goto IL_0031_1;
				}
			}
			{
				//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:470>
				StringBuilder_t* L_10 = __this->___builder;
				NullCheck(L_10);
				StringBuilder_t* L_11;
				L_11 = StringBuilder_Append_m71228B30F05724CD2CD96D9611DCD61BFB96A6E1(L_10, ((int32_t)44), NULL);
			}

IL_0031_1:
			{
				//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:473>
				RuntimeObject* L_12 = V_2;
				Serializer_SerializeValue_m952078ABBCE9FDAF9FC44028879BBF5FC10C1030(__this, L_12, NULL);
				//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:475>
				V_0 = (bool)0;
			}

IL_003a_1:
			{
				//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:468>
				RuntimeObject* L_13 = V_1;
				NullCheck(L_13);
				bool L_14;
				L_14 = InterfaceFuncInvoker0< bool >::Invoke(0, IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA_il2cpp_TypeInfo_var, L_13);
				if (L_14)
				{
					goto IL_0019_1;
				}
			}
			{
				goto IL_0055;
			}
		}
		catch(Il2CppExceptionWrapper& e)
		{
			__finallyBlock.StoreException(e.ex);
		}
	}

IL_0055:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:478>
		StringBuilder_t* L_15 = __this->___builder;
		NullCheck(L_15);
		StringBuilder_t* L_16;
		L_16 = StringBuilder_Append_m71228B30F05724CD2CD96D9611DCD61BFB96A6E1(L_15, ((int32_t)93), NULL);
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:479>
		return;
	}
}
// Method Definition Index: 79948
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Serializer_SerializeString_mB658D7F56B6D171FE905FE62A98E59DAC6050BDB (Serializer_t080A530C38A3C9BBE2EB8CB81F36149BCFE25635* __this, String_t* ___0_str, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Convert_t7097FF336D592F7C06D88A98349A44646F91EFFC_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral5962E944D7340CE47999BF097B4AFD70C1501FB9);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral785F17F45C331C415D0A7458E6AAC36966399C51);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral7F3238CD8C342B06FB9AB185C610175C84625462);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral848E5ED630B3142F565DD995C6E8D30187ED33CD);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralA7C3FCA8C63E127B542B38A5CA5E3FEEDDD1B122);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralB78F235D4291950A7D101307609C259F3E1F033F);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralDA666908BB15F4E1D2649752EC5DCBD0D5C64699);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralE727BF366E3CC855B808D806440542BF7152AF19);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralF18840F490E42D3CE48CDCBF47229C1C240F8ABE);
		s_Il2CppMethodInitialized = true;
	}
	CharU5BU5D_t799905CF001DD5F13F7DBB310181FC4D8B7D0AAB* V_0 = NULL;
	int32_t V_1 = 0;
	Il2CppChar V_2 = 0x0;
	int32_t V_3 = 0;
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:482>
		StringBuilder_t* L_0 = __this->___builder;
		NullCheck(L_0);
		StringBuilder_t* L_1;
		L_1 = StringBuilder_Append_m71228B30F05724CD2CD96D9611DCD61BFB96A6E1(L_0, ((int32_t)34), NULL);
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:484>
		String_t* L_2 = ___0_str;
		NullCheck(L_2);
		CharU5BU5D_t799905CF001DD5F13F7DBB310181FC4D8B7D0AAB* L_3;
		L_3 = String_ToCharArray_m0699A92AA3E744229EF29CB9D943C47DF4FE5B46(L_2, NULL);
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:485>
		V_0 = L_3;
		V_1 = 0;
		goto IL_012d;
	}

IL_001c:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:485>
		CharU5BU5D_t799905CF001DD5F13F7DBB310181FC4D8B7D0AAB* L_4 = V_0;
		int32_t L_5 = V_1;
		NullCheck(L_4);
		int32_t L_6 = L_5;
		uint16_t L_7 = (uint16_t)(L_4)->GetAt(static_cast<il2cpp_array_size_t>(L_6));
		V_2 = L_7;
		Il2CppChar L_8 = V_2;
		switch (((int32_t)il2cpp_codegen_subtract((int32_t)L_8, 8)))
		{
			case 0:
			{
				goto IL_007b;
			}
			case 1:
			{
				goto IL_00cd;
			}
			case 2:
			{
				goto IL_00a7;
			}
			case 3:
			{
				goto IL_00e0;
			}
			case 4:
			{
				goto IL_0091;
			}
			case 5:
			{
				goto IL_00ba;
			}
		}
	}
	{
		Il2CppChar L_9 = V_2;
		if ((((int32_t)L_9) == ((int32_t)((int32_t)34))))
		{
			goto IL_004f;
		}
	}
	{
		Il2CppChar L_10 = V_2;
		if ((((int32_t)L_10) == ((int32_t)((int32_t)92))))
		{
			goto IL_0065;
		}
	}
	{
		goto IL_00e0;
	}

IL_004f:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:488>
		StringBuilder_t* L_11 = __this->___builder;
		NullCheck(L_11);
		StringBuilder_t* L_12;
		L_12 = StringBuilder_Append_m08904D74E0C78E5F36DCD9C9303BDD07886D9F7D(L_11, _stringLiteral848E5ED630B3142F565DD995C6E8D30187ED33CD, NULL);
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:489>
		goto IL_0129;
	}

IL_0065:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:491>
		StringBuilder_t* L_13 = __this->___builder;
		NullCheck(L_13);
		StringBuilder_t* L_14;
		L_14 = StringBuilder_Append_m08904D74E0C78E5F36DCD9C9303BDD07886D9F7D(L_13, _stringLiteralF18840F490E42D3CE48CDCBF47229C1C240F8ABE, NULL);
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:492>
		goto IL_0129;
	}

IL_007b:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:494>
		StringBuilder_t* L_15 = __this->___builder;
		NullCheck(L_15);
		StringBuilder_t* L_16;
		L_16 = StringBuilder_Append_m08904D74E0C78E5F36DCD9C9303BDD07886D9F7D(L_15, _stringLiteral5962E944D7340CE47999BF097B4AFD70C1501FB9, NULL);
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:495>
		goto IL_0129;
	}

IL_0091:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:497>
		StringBuilder_t* L_17 = __this->___builder;
		NullCheck(L_17);
		StringBuilder_t* L_18;
		L_18 = StringBuilder_Append_m08904D74E0C78E5F36DCD9C9303BDD07886D9F7D(L_17, _stringLiteralA7C3FCA8C63E127B542B38A5CA5E3FEEDDD1B122, NULL);
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:498>
		goto IL_0129;
	}

IL_00a7:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:500>
		StringBuilder_t* L_19 = __this->___builder;
		NullCheck(L_19);
		StringBuilder_t* L_20;
		L_20 = StringBuilder_Append_m08904D74E0C78E5F36DCD9C9303BDD07886D9F7D(L_19, _stringLiteral785F17F45C331C415D0A7458E6AAC36966399C51, NULL);
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:501>
		goto IL_0129;
	}

IL_00ba:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:503>
		StringBuilder_t* L_21 = __this->___builder;
		NullCheck(L_21);
		StringBuilder_t* L_22;
		L_22 = StringBuilder_Append_m08904D74E0C78E5F36DCD9C9303BDD07886D9F7D(L_21, _stringLiteralB78F235D4291950A7D101307609C259F3E1F033F, NULL);
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:504>
		goto IL_0129;
	}

IL_00cd:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:506>
		StringBuilder_t* L_23 = __this->___builder;
		NullCheck(L_23);
		StringBuilder_t* L_24;
		L_24 = StringBuilder_Append_m08904D74E0C78E5F36DCD9C9303BDD07886D9F7D(L_23, _stringLiteral7F3238CD8C342B06FB9AB185C610175C84625462, NULL);
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:507>
		goto IL_0129;
	}

IL_00e0:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:509>
		Il2CppChar L_25 = V_2;
		il2cpp_codegen_runtime_class_init_inline(Convert_t7097FF336D592F7C06D88A98349A44646F91EFFC_il2cpp_TypeInfo_var);
		int32_t L_26;
		L_26 = Convert_ToInt32_mDBBE9318A7CCE1560974CE93F5BFED9931CF0052(L_25, NULL);
		V_3 = L_26;
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:510>
		int32_t L_27 = V_3;
		if ((((int32_t)L_27) < ((int32_t)((int32_t)32))))
		{
			goto IL_0100;
		}
	}
	{
		int32_t L_28 = V_3;
		if ((((int32_t)L_28) > ((int32_t)((int32_t)126))))
		{
			goto IL_0100;
		}
	}
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:511>
		StringBuilder_t* L_29 = __this->___builder;
		Il2CppChar L_30 = V_2;
		NullCheck(L_29);
		StringBuilder_t* L_31;
		L_31 = StringBuilder_Append_m71228B30F05724CD2CD96D9611DCD61BFB96A6E1(L_29, L_30, NULL);
		goto IL_0129;
	}

IL_0100:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:513>
		StringBuilder_t* L_32 = __this->___builder;
		NullCheck(L_32);
		StringBuilder_t* L_33;
		L_33 = StringBuilder_Append_m08904D74E0C78E5F36DCD9C9303BDD07886D9F7D(L_32, _stringLiteralDA666908BB15F4E1D2649752EC5DCBD0D5C64699, NULL);
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:514>
		StringBuilder_t* L_34 = __this->___builder;
		String_t* L_35;
		L_35 = Int32_ToString_m967AECC237535C552A97A80C7875E31B98496CA9((&V_3), _stringLiteralE727BF366E3CC855B808D806440542BF7152AF19, NULL);
		NullCheck(L_34);
		StringBuilder_t* L_36;
		L_36 = StringBuilder_Append_m08904D74E0C78E5F36DCD9C9303BDD07886D9F7D(L_34, L_35, NULL);
	}

IL_0129:
	{
		int32_t L_37 = V_1;
		V_1 = ((int32_t)il2cpp_codegen_add(L_37, 1));
	}

IL_012d:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:485>
		int32_t L_38 = V_1;
		CharU5BU5D_t799905CF001DD5F13F7DBB310181FC4D8B7D0AAB* L_39 = V_0;
		NullCheck(L_39);
		if ((((int32_t)L_38) < ((int32_t)((int32_t)(((RuntimeArray*)L_39)->max_length)))))
		{
			goto IL_001c;
		}
	}
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:520>
		StringBuilder_t* L_40 = __this->___builder;
		NullCheck(L_40);
		StringBuilder_t* L_41;
		L_41 = StringBuilder_Append_m71228B30F05724CD2CD96D9611DCD61BFB96A6E1(L_40, ((int32_t)34), NULL);
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:521>
		return;
	}
}
// Method Definition Index: 79949
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Serializer_SerializeOther_m5A394D1B6C91B809DFBF69B4F3B9FAE531853841 (Serializer_t080A530C38A3C9BBE2EB8CB81F36149BCFE25635* __this, RuntimeObject* ___0_value, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Convert_t7097FF336D592F7C06D88A98349A44646F91EFFC_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Decimal_tDA6C877282B2D789CF97C0949661CC11D643969F_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral2C3D4826D5236B3C9A914C5CE2E3D8CEA48AC7CE);
		s_Il2CppMethodInitialized = true;
	}
	float V_0 = 0.0f;
	double V_1 = 0.0;
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:527>
		RuntimeObject* L_0 = ___0_value;
		if (!((RuntimeObject*)IsInstSealed((RuntimeObject*)L_0, il2cpp_defaults.single_class)))
		{
			goto IL_0028;
		}
	}
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:528>
		StringBuilder_t* L_1 = __this->___builder;
		RuntimeObject* L_2 = ___0_value;
		V_0 = ((*(float*)UnBox(L_2, il2cpp_defaults.single_class)));
		String_t* L_3;
		L_3 = Single_ToString_m3F2C4433B6ADFA5ED8E3F14ED19CD23014E5179D((&V_0), _stringLiteral2C3D4826D5236B3C9A914C5CE2E3D8CEA48AC7CE, NULL);
		NullCheck(L_1);
		StringBuilder_t* L_4;
		L_4 = StringBuilder_Append_m08904D74E0C78E5F36DCD9C9303BDD07886D9F7D(L_1, L_3, NULL);
		return;
	}

IL_0028:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:529>
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:530>
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:531>
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:532>
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:533>
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:534>
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:535>
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:536>
		RuntimeObject* L_5 = ___0_value;
		if (((RuntimeObject*)IsInstSealed((RuntimeObject*)L_5, il2cpp_defaults.int32_class)))
		{
			goto IL_0068;
		}
	}
	{
		RuntimeObject* L_6 = ___0_value;
		if (((RuntimeObject*)IsInstSealed((RuntimeObject*)L_6, il2cpp_defaults.uint32_class)))
		{
			goto IL_0068;
		}
	}
	{
		RuntimeObject* L_7 = ___0_value;
		if (((RuntimeObject*)IsInstSealed((RuntimeObject*)L_7, il2cpp_defaults.int64_class)))
		{
			goto IL_0068;
		}
	}
	{
		RuntimeObject* L_8 = ___0_value;
		if (((RuntimeObject*)IsInstSealed((RuntimeObject*)L_8, il2cpp_defaults.sbyte_class)))
		{
			goto IL_0068;
		}
	}
	{
		RuntimeObject* L_9 = ___0_value;
		if (((RuntimeObject*)IsInstSealed((RuntimeObject*)L_9, il2cpp_defaults.byte_class)))
		{
			goto IL_0068;
		}
	}
	{
		RuntimeObject* L_10 = ___0_value;
		if (((RuntimeObject*)IsInstSealed((RuntimeObject*)L_10, il2cpp_defaults.int16_class)))
		{
			goto IL_0068;
		}
	}
	{
		RuntimeObject* L_11 = ___0_value;
		if (((RuntimeObject*)IsInstSealed((RuntimeObject*)L_11, il2cpp_defaults.uint16_class)))
		{
			goto IL_0068;
		}
	}
	{
		RuntimeObject* L_12 = ___0_value;
		if (!((RuntimeObject*)IsInstSealed((RuntimeObject*)L_12, il2cpp_defaults.uint64_class)))
		{
			goto IL_0076;
		}
	}

IL_0068:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:537>
		StringBuilder_t* L_13 = __this->___builder;
		RuntimeObject* L_14 = ___0_value;
		NullCheck(L_13);
		StringBuilder_t* L_15;
		L_15 = StringBuilder_Append_m3A7D629DAA5E0E36B8A617A911E34F79AF84AE63(L_13, L_14, NULL);
		return;
	}

IL_0076:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:538>
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:539>
		RuntimeObject* L_16 = ___0_value;
		if (((RuntimeObject*)IsInstSealed((RuntimeObject*)L_16, il2cpp_defaults.double_class)))
		{
			goto IL_0086;
		}
	}
	{
		RuntimeObject* L_17 = ___0_value;
		if (!((RuntimeObject*)IsInstSealed((RuntimeObject*)L_17, Decimal_tDA6C877282B2D789CF97C0949661CC11D643969F_il2cpp_TypeInfo_var)))
		{
			goto IL_00a6;
		}
	}

IL_0086:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:540>
		StringBuilder_t* L_18 = __this->___builder;
		RuntimeObject* L_19 = ___0_value;
		il2cpp_codegen_runtime_class_init_inline(Convert_t7097FF336D592F7C06D88A98349A44646F91EFFC_il2cpp_TypeInfo_var);
		double L_20;
		L_20 = Convert_ToDouble_m86FF4F837721833186E883102C056A35F0860EB0(L_19, NULL);
		V_1 = L_20;
		String_t* L_21;
		L_21 = Double_ToString_m70EC76E1DAD7E8B5B47AF9292189BF3711B24B75((&V_1), _stringLiteral2C3D4826D5236B3C9A914C5CE2E3D8CEA48AC7CE, NULL);
		NullCheck(L_18);
		StringBuilder_t* L_22;
		L_22 = StringBuilder_Append_m08904D74E0C78E5F36DCD9C9303BDD07886D9F7D(L_18, L_21, NULL);
		return;
	}

IL_00a6:
	{
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:542>
		RuntimeObject* L_23 = ___0_value;
		NullCheck(L_23);
		String_t* L_24;
		L_24 = VirtualFuncInvoker0< String_t* >::Invoke(3, L_23);
		Serializer_SerializeString_mB658D7F56B6D171FE905FE62A98E59DAC6050BDB(__this, L_24, NULL);
		//<source_info:D:/GitHub/Dice-Rush/Assets/Branch/MiniJSON.cs:544>
		return;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
