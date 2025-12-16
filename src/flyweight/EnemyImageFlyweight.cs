using Serilog;

public static class EnemyImageFlyweight
{
    private static readonly Dictionary<string, byte[]> _imageBytesByType = new();

    public static byte[] GetOrLoad(string enemyType, string imagePath)
    {
        if (_imageBytesByType.TryGetValue(enemyType, out var bytes))
            return bytes;

        if (!File.Exists(imagePath))
            throw new FileNotFoundException($"Enemy image not found: {imagePath}");

        bytes = File.ReadAllBytes(imagePath);
        _imageBytesByType[enemyType] = bytes;

        Log.Debug($"Loaded bytes for enemy: {enemyType}, size: {bytes.Length}");
        return bytes;
    }
    public static void ClearCache() => _imageBytesByType.Clear();
}