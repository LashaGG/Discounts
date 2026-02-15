// Copyright (C) TBC Bank. All Rights Reserved.

namespace Discounts.API.Requests;

public class UpdateRoleRequest
{
    public string Role { get; set; } = string.Empty;
}

public class RejectDiscountRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class UpdateSettingRequest
{
    public string Value { get; set; } = string.Empty;
}
