﻿. $PSScriptRoot\Support\Standalone.ps1

Describe "Set-ChannelProperty" -Tag @("PowerShell", "UnitTest") {

    SetMultiTypeResponse

    $channel = Get-Sensor | Get-Channel

    It "sets a property with an invalid type" {
        $timeSpan = New-TimeSpan -Seconds 10

        { $channel | Set-ChannelProperty LimitsEnabled $timeSpan } | Should Throw "Expected type: 'System.Boolean'. Actual type: 'System.TimeSpan'"
    }

    It "sets a property with an empty string" {
        $channel | Set-ChannelProperty LowerErrorLimit ""
    }

    It "sets a property with null on a type that allows null" {
        $channel | Set-ChannelProperty LowerErrorLimit $null
    }

    It "sets a property with null on a type that disallows null" {
        { $channel | Set-ChannelProperty ColorMode $null } | Should Throw "Null may only be assigned to properties of type string, int and double"
    }

    It "sets a nullable type with its underlying type" {
        $val = $true
        $val.GetType() | Should Be "bool"

        $channel | Set-ChannelProperty LimitsEnabled $val
    }

    It "requires Value be specified" {
        { $channel | Set-ChannelProperty UpperErrorLimit } | Should Throw "Value parameter is mandatory"
    }

    It "setting an invalid enum value lists all valid possibilities" {

        $expected = "'banana' is not a valid value for enum AutoMode. Please specify one of 'Automatic' or 'Manual'"

        { $channel | Set-ChannelProperty ColorMode "banana" } | Should Throw $expected
    }

    It "passes through with -Batch:`$false" {
        SetMultiTypeResponse

        $channel = Get-Sensor -Count 1 | Get-Channel

        $newChannel = $channel | Set-ChannelProperty LimitsEnabled $false -PassThru -Batch:$false

        $newChannel | Should Be $channel
    }

    It "passes through with -Batch:`$true" {
        SetMultiTypeResponse

        $channel = Get-Sensor -Count 1 | Get-Channel

        $newChannel = $channel | Set-ChannelProperty LimitsEnabled $false -PassThru -Batch:$true

        $newChannel | Should Be $channel
    }

    Context "Default" {

        It "sets a property with a valid type" {
            
            SetAddressValidatorResponse @(
                "api/getstatus.htm?id=0&"
                "editsettings?id=4000,4001&limiterrormsg_1=oh+no!&limitmode_1=1&"
            )

            $channel | Set-ChannelProperty ErrorLimitMessage "oh no!"
        }

        $versionCases = @(
            @{version = "17.3"; address = "id=4000,4001&limiterrormsg_1=tomato&limitmode_1=1"}
            @{version = "18.1"; address = "id=4000,4001&limiterrormsg_1=tomato&limitmode_1=1&limitmaxerror_1=100"}
        )

        It "sets a version specific property on version <version>" -TestCases $versionCases {

            param($version, $address)

            SetAddressValidatorResponse $address

            SetVersion $version

            $channel | Set-ChannelProperty ErrorLimitMessage "tomato"
        }

        It "executes with -Batch:`$true" {

            $channel.Count | Should Be 2

            SetAddressValidatorResponse @(
                "api/getstatus.htm?id=0&"
                "editsettings?id=4000,4001&limiterrormsg_1=oh+no!&limitmode_1=1&"
            )

            $channel | Set-ChannelProperty ErrorLimitMessage "oh no!" -Batch:$true
        }

        It "executes with -Batch:`$false" {

            $channel.Count | Should Be 2

            SetAddressValidatorResponse @(
                "api/getstatus.htm?id=0&"
                "editsettings?id=4000&limiterrormsg_1=oh+no!&limitmode_1=1&"
                "editsettings?id=4001&limiterrormsg_1=oh+no!&limitmode_1=1&"
            )

            $channel | Set-ChannelProperty ErrorLimitMessage "oh no!" -Batch:$false
        }

        It "sets multiple properties with -Batch:`$true" {
            $channel.Count | Should Be 2

            SetAddressValidatorResponse "id=4000,4001&limitmaxerror_1=100&limitminerror_1=20&limitmode_1=1&valuelookup_1=test"

            $channel | Set-ChannelProperty -UpperErrorLimit 100 -LowerErrorLimit 20 -ValueLookup test
        }

        It "removes all but the last instance of a parameter" {

            $channel.Count | Should Be 2

            SetAddressValidatorResponse @(
                "api/getstatus.htm?id=0&"
                "editsettings?id=4000,4001&limitmode_1=0&limitmaxerror_1=&limitmaxwarning_1=&limitminerror_1=&limitminwarning_1=&limiterrormsg_1=&limitwarningmsg_1=&"
            )

            $channel | Set-ChannelProperty -UpperErrorLimit 100 -LowerErrorLimit 20 -LimitsEnabled $false
        }

        It "sets multiple properties with -Batch:`$false" {

            $channel.Count | Should Be 2

            SetAddressValidatorResponse @(
                "api/getstatus.htm?id=0&"
                "editsettings?id=4000&limitmode_1=0&limitmaxerror_1=&limitmaxwarning_1=&limitminerror_1=&limitminwarning_1=&limiterrormsg_1=&limitwarningmsg_1=&"
                "editsettings?id=4001&limitmode_1=0&limitmaxerror_1=&limitmaxwarning_1=&limitminerror_1=&limitminwarning_1=&limiterrormsg_1=&limitwarningmsg_1=&"
            )

            $channel | Set-ChannelProperty -UpperErrorLimit 100 -LowerErrorLimit 20 -LimitsEnabled $false -Batch:$false
        }

        It "doesn't specify any dynamic parameters" {
            { $channel | Set-ObjectProperty } | Should Throw "Cannot process command because of one or more missing mandatory parameters: Property"
        }

        It "splats dynamic properties" {

            $channel.Count | Should Be 2

            SetAddressValidatorResponse "id=4000,4001&limitmaxerror_1=100&valuelookup_1=test&limitminerror_1=20&limitmode_1=1"

            $splat = @{
                UpperErrorLimit = 100
                LowerErrorLimit = 20
                ValueLookup = "test"
            }

            $channel | Set-ChannelProperty @splat
        }
    }

    Context "Manual" {
        It "sets a property using the manual parameter set" {

            SetAddressValidatorResponse @(
                "api/getstatus.htm?id=0&"
                "editsettings?id=1001&limitmode_1=1&"
            )

            Set-ChannelProperty -SensorId 1001 -ChannelId 1 LimitsEnabled $true
        }

        $versionCases = @(
            @{version = "17.3"; address = "editsettings?id=1001&limiterrormsg_1=hello&limitmode_1=1"}
            @{version = "18.1"; address = @(
                    "api/table.xml?content=channels&columns=lastvalue,objid,name&count=*&id=1001&"
                    "controls/channeledit.htm?id=1001&channel=1&"
                    "editsettings?id=1001&limiterrormsg_1=hello&limitmode_1=1&limitmaxerror_1=100&"
                )
            }
        )

        It "sets a version specific property on version <version>" -TestCases $versionCases {

            param($version, $address)

            SetAddressValidatorResponse $address

            SetVersion $version

            Set-ChannelProperty -SensorId 1001 -ChannelId 1 ErrorLimitMessage "hello"
        }

        It "throws modifying an invalid channel ID on a version specific property" {

            SetMultiTypeResponse

            SetVersion "18.1"

            { Set-ChannelProperty -SensorId 1001 -ChannelId 2 ErrorLimitMessage "hello" } | Should Throw "Channel ID 2 does not exist on sensor ID 1001"
        }

        It "doesn't retrieve channels when setting a version specific property and a limit is included" {

            SetAddressValidatorResponse "id=2002&limiterrormsg_2=hello&limitminerror_2=5&limitmode_2=1"

            SetVersion "18.1"

            Set-ChannelProperty -SensorId 2002 -ChannelId 2 -ErrorLimitMessage "hello" -LowerErrorLimit 5
        }

        It "executes with -Batch:`$true" {
            
            SetAddressValidatorResponse @(
                "api/getstatus.htm?id=0&"
                "editsettings?id=1001&limitmode_1=1&"
            )

            Set-ChannelProperty -SensorId 1001 -ChannelId 1 -Batch:$true LimitsEnabled $true
        }

        It "executes with -Batch:`$false" {
            SetAddressValidatorResponse @(
                "api/getstatus.htm?id=0&"
                "editsettings?id=1001&limitmode_1=1&"
            )

            Set-ChannelProperty -SensorId 1001 -ChannelId 1 -Batch:$false LimitsEnabled $true
        }

        It "sets multiple properties" {

            SetAddressValidatorResponse "id=1001&limitmaxerror_2=100&limitminerror_2=20&limitmode_2=1&valuelookup_2=test"

            Set-ChannelProperty -SensorId 1001 -ChannelId 2 -UpperErrorLimit 100 -LowerErrorLimit 20 -ValueLookup test
        }

        It "doesn't specify any dynamic parameters" {
            { Set-ChannelProperty } | Should Throw "Cannot process command because of one or more missing mandatory parameters: Channel Property"
        }

        It "splats dynamic parameters" {

            SetAddressValidatorResponse "id=1001&limitminerror_2=20&limitmaxerror_2=100&limitmode_2=1&valuelookup_2=test"

            $splat = @{
                SensorId = 1001
                ChannelId = 2
                UpperErrorLimit = 100
                LowerErrorLimit = 20
                ValueLookup = "test"
            }

            Set-ChannelProperty @splat
        }
    }
}