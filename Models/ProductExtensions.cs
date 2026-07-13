using System.Text.Json;

namespace TMDTStore.Models;

/// <summary>
/// Một nhóm thông số kỹ thuật để hiển thị theo danh mục.
/// </summary>
public class ProductSpecGroup
{
    public string Name { get; set; } = "";
    public List<Dictionary<string, string>> Specs { get; set; } = new();
}

public static class ProductExtensions
{
    /// <summary>
    /// Parse TechnicalSpecs JSON ở cả 2 dạng:
    /// 1) [{ "key": "...", "value": "..." }, ...] — editor Admin
    /// 2) ["CPU: Intel...", "RAM: 16GB", ...] — sync Phong Vũ / import
    /// </summary>
    public static List<Dictionary<string, string>> ParseTechnicalSpecsJson(string? json)
    {
        var specs = new List<Dictionary<string, string>>();
        if (string.IsNullOrWhiteSpace(json))
            return specs;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return specs;

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind == JsonValueKind.Object)
                {
                    var key = el.TryGetProperty("key", out var k) ? k.GetString()
                        : el.TryGetProperty("name", out var n) ? n.GetString()
                        : null;
                    var value = el.TryGetProperty("value", out var v) ? v.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(key) && value != null)
                    {
                        specs.Add(new Dictionary<string, string>
                        {
                            ["key"] = key.Trim(),
                            ["value"] = value.Trim()
                        });
                    }
                    continue;
                }

                if (el.ValueKind == JsonValueKind.String)
                {
                    var line = el.GetString()?.Trim();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    var sep = line.IndexOf(':');
                    if (sep < 0)
                        sep = line.IndexOf('：'); // fullwidth colon

                    if (sep > 0)
                    {
                        specs.Add(new Dictionary<string, string>
                        {
                            ["key"] = line[..sep].Trim(),
                            ["value"] = line[(sep + 1)..].Trim()
                        });
                    }
                    else
                    {
                        specs.Add(new Dictionary<string, string>
                        {
                            ["key"] = "Thông số",
                            ["value"] = line
                        });
                    }
                }
            }
        }
        catch
        {
            // ignore malformed JSON
        }

        return specs;
    }

    /// <summary>
    /// Lấy danh sách thông số kỹ thuật đã được gộp tự động từ thông tin sản phẩm
    /// (thương hiệu, bảo hành, ...) cùng với thông số do người dùng nhập trong TechnicalSpecs.
    /// Tránh trùng lặp — nếu người dùng đã tự nhập thì không thêm tự động.
    /// </summary>
    public static List<Dictionary<string, string>> GetMergedTechnicalSpecs(this Product product)
    {
        var specs = ParseTechnicalSpecsJson(product.TechnicalSpecs);

        // Xây tập key đã có (để tránh trùng lặp)
        var existingKeys = new HashSet<string>(
            specs.Select(s => s.GetValueOrDefault("key", "").Trim().ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase
        );

        // 2. Tự động thêm "Thương hiệu" từ Brand (nếu chưa có)
        var brandName = product.Brand?.Name ?? product.BrandName;
        if (!string.IsNullOrEmpty(brandName) && !existingKeys.Contains("thương hiệu"))
        {
            specs.Insert(0, new Dictionary<string, string>
            {
                { "key", "Thương hiệu" },
                { "value", brandName }
            });
        }

        // 3. Tự động thêm "Bảo hành" từ WarrantyMonths (nếu chưa có)
        if (product.WarrantyMonths.HasValue && product.WarrantyMonths > 0 && !existingKeys.Contains("bảo hành"))
        {
            specs.Add(new Dictionary<string, string>
            {
                { "key", "Bảo hành" },
                { "value", $"{product.WarrantyMonths} tháng" }
            });
        }

        return specs;
    }

    /// <summary>
    /// Lấy thông số kỹ thuật được phân loại thành 2 nhóm:
    /// 1. Thông số chung: tên sản phẩm, thương hiệu, bảo hành
    /// 2. Cấu hình chi tiết: thuộc tính biến thể + thông số cấu hình chung
    /// </summary>
    public static List<ProductSpecGroup> GetGroupedTechnicalSpecs(this Product product)
    {
        var groups = new List<ProductSpecGroup>();

        // ====== NHÓM 1: THÔNG SỐ CHUNG ======
        var generalSpecs = new List<Dictionary<string, string>>();

        // Tên sản phẩm
        if (!string.IsNullOrEmpty(product.Name))
        {
            generalSpecs.Add(new Dictionary<string, string>
            {
                { "key", "Tên sản phẩm" },
                { "value", product.Name }
            });
        }

        // Thương hiệu
        var brandName = product.Brand?.Name ?? product.BrandName;
        if (!string.IsNullOrEmpty(brandName))
        {
            generalSpecs.Add(new Dictionary<string, string>
            {
                { "key", "Thương hiệu" },
                { "value", brandName }
            });
        }

        // Bảo hành
        if (product.WarrantyMonths.HasValue && product.WarrantyMonths > 0)
        {
            generalSpecs.Add(new Dictionary<string, string>
            {
                { "key", "Bảo hành" },
                { "value", $"{product.WarrantyMonths} tháng" }
            });
        }

        if (generalSpecs.Count > 0)
        {
            groups.Add(new ProductSpecGroup { Name = "Thông số chung", Specs = generalSpecs });
        }

        // ====== NHÓM 2: CẤU HÌNH CHI TIẾT ======
        var detailSpecs = new List<Dictionary<string, string>>();

        // Tập key để tránh trùng lặp trong nhóm này
        var detailKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 2a. Thuộc tính từ biến thể đầu tiên (màu sắc, option)
        var firstVariant = product.ProductVariants?.FirstOrDefault();
        if (firstVariant != null && !string.IsNullOrEmpty(firstVariant.Attributes))
        {
            try
            {
                var variantAttrs = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(firstVariant.Attributes);
                if (variantAttrs != null)
                {
                    foreach (var attr in variantAttrs)
                    {
                        var key = attr.GetValueOrDefault("key", "").Trim();
                        if (!string.IsNullOrEmpty(key) && !detailKeys.Contains(key))
                        {
                            detailSpecs.Add(attr);
                            detailKeys.Add(key);
                        }
                    }
                }
            }
            catch { }
        }

        // 2b. Thông số cấu hình chung từ TechnicalSpecs (bỏ qua key đã có từ biến thể)
        var userSpecs = ParseTechnicalSpecsJson(product.TechnicalSpecs);
        if (userSpecs.Count > 0)
        {
            // Bỏ qua các thông số đã thuộc nhóm chung
            var skipKeys = new HashSet<string> { "thương hiệu", "bảo hành", "tên sản phẩm", "product name" };

            foreach (var spec in userSpecs)
            {
                var key = spec.GetValueOrDefault("key", "").Trim();
                if (string.IsNullOrEmpty(key)) continue;
                if (skipKeys.Contains(key.ToLowerInvariant())) continue;
                if (!detailKeys.Contains(key))
                {
                    detailSpecs.Add(spec);
                    detailKeys.Add(key);
                }
            }
        }

        if (detailSpecs.Count > 0)
        {
            groups.Add(new ProductSpecGroup { Name = "Cấu hình chi tiết", Specs = detailSpecs });
        }

        return groups;
    }
}
