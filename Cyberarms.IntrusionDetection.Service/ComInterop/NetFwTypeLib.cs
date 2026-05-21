namespace NetFwTypeLib
{
    public enum NET_FW_ACTION_
    {
        NET_FW_ACTION_BLOCK = 0,
        NET_FW_ACTION_ALLOW = 1,
        NET_FW_ACTION_MAX = 2
    }

    public enum NET_FW_IP_PROTOCOL_
    {
        NET_FW_IP_PROTOCOL_TCP = 6,
        NET_FW_IP_PROTOCOL_UDP = 17,
        NET_FW_IP_PROTOCOL_ANY = 256
    }

    public enum NET_FW_SCOPE_
    {
        NET_FW_SCOPE_ALL = 0,
        NET_FW_SCOPE_LOCAL_SUBNET = 1,
        NET_FW_SCOPE_CUSTOM = 2,
        NET_FW_SCOPE_MAX = 3
    }

    public enum NET_FW_RULE_DIRECTION_
    {
        NET_FW_RULE_DIR_IN = 1,
        NET_FW_RULE_DIR_OUT = 2,
        NET_FW_RULE_DIR_MAX = 3
    }
}
