namespace SerialPortTool;

public enum CrcType
{
    CRC8,
    CRC16,
    CRC32
}

public static class CrcHelper
{
    public static byte[] Calculate(byte[] data, CrcType type)
    {
        return type switch
        {
            CrcType.CRC8 => new byte[] { CalculateCRC8(data) },
            CrcType.CRC16 => CalculateCRC16(data),
            CrcType.CRC32 => CalculateCRC32(data),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    public static bool Verify(byte[] data, byte[] crc, CrcType type)
    {
        var calculated = Calculate(data, type);
        if (calculated.Length != crc.Length)
            return false;

        for (int i = 0; i < calculated.Length; i++)
        {
            if (calculated[i] != crc[i])
                return false;
        }
        return true;
    }

    public static byte CalculateCRC8(byte[] data)
    {
        byte crc = 0x00;
        foreach (byte b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 0x80) != 0)
                {
                    crc = (byte)((crc << 1) ^ 0x07);
                }
                else
                {
                    crc <<= 1;
                }
            }
        }
        return crc;
    }

    public static byte[] CalculateCRC16(byte[] data)
    {
        ushort crc = 0xFFFF;
        foreach (byte b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 0x0001) != 0)
                {
                    crc = (ushort)((crc >> 1) ^ 0xA001);
                }
                else
                {
                    crc >>= 1;
                }
            }
        }
        return new byte[] { (byte)(crc & 0xFF), (byte)(crc >> 8) };
    }

    public static byte[] CalculateCRC32(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        uint polynomial = 0xEDB88320;

        foreach (byte b in data)
        {
            uint temp = (crc ^ b) & 0xFF;
            for (int i = 0; i < 8; i++)
            {
                if ((temp & 1) != 0)
                {
                    temp = (temp >> 1) ^ polynomial;
                }
                else
                {
                    temp >>= 1;
                }
            }
            crc = (crc >> 8) ^ temp;
        }

        crc ^= 0xFFFFFFFF;
        return new byte[] {
            (byte)(crc & 0xFF),
            (byte)((crc >> 8) & 0xFF),
            (byte)((crc >> 16) & 0xFF),
            (byte)((crc >> 24) & 0xFF)
        };
    }

    public static string BytesToHex(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", " ");
    }
}