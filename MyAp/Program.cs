using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Npgsql;
using System.Globalization;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Configuração do CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Habilitar o CORS
app.UseCors();

app.UseRouting();

app.MapPost("/remover", async (HttpContext httpContext) =>
{
    try
    {
        var requestBody = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
        var formData = JsonSerializer.Deserialize<FormData>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Verificação de entrada
        if (formData == null || formData.Codigo <= 0)
        {
            httpContext.Response.StatusCode = 400; // Bad Request
            await httpContext.Response.WriteAsync("Código inválido.");
            return;
        }

        string connectionString = "Host=172.30.226.119;Port=5433;Username=postgres;Password=123456;Database=bancodb";

        using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();

            var sql = "UPDATE public.doador SET situacao = @situacao WHERE codigo = @codigo";

            using (var command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("situacao", "INATIVO"); // Alterar para "INATIVO"
                command.Parameters.AddWithValue("codigo", formData.Codigo);

                int rowsAffected = await command.ExecuteNonQueryAsync();

                // Verificar se a operação foi bem-sucedida
                if (rowsAffected > 0)
                {
                    await httpContext.Response.WriteAsync("Doador desativado com sucesso.");
                }
                else
                {
                    httpContext.Response.StatusCode = 404; // Not Found
                    await httpContext.Response.WriteAsync("Doador não encontrado.");
                }
            }
        }
    }
    catch (Exception ex)
    {
        httpContext.Response.StatusCode = 500;
        await httpContext.Response.WriteAsync($"Erro ao processar a requisição: {ex.Message}\n{ex.StackTrace}");
    }
});


app.MapPost("/alterar", async (HttpContext httpContext) =>
{
    try
    {
        var requestBody = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
        var formData = JsonSerializer.Deserialize<FormData>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Console.WriteLine("Dados recebidos para alteração:");
        Console.WriteLine(JsonSerializer.Serialize(formData));

        if (string.IsNullOrEmpty(formData.Nome) ||
            string.IsNullOrEmpty(formData.Cpf) ||
            string.IsNullOrEmpty(formData.Contato) ||
            string.IsNullOrEmpty(formData.TipoSanguineo) ||
            string.IsNullOrEmpty(formData.Rh))
        {
            httpContext.Response.StatusCode = 400; // Bad Request
            await httpContext.Response.WriteAsync("Todos os campos obrigatórios devem ser preenchidos.");
            return;
        }

        string connectionString = "Host=172.30.226.119;Port=5433;Username=postgres;Password=123456;Database=bancodb";

        using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();

            var sql = "UPDATE public.doador SET nome = @nome, cpf = @cpf, contato = @contato, tipo_sanguineo = @tipo_sanguineo, rh = @rh, situacao = @situacao WHERE codigo = @codigo";

            using (var command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("nome", formData.Nome);
                command.Parameters.AddWithValue("cpf", formData.Cpf);
                command.Parameters.AddWithValue("contato", formData.Contato);
                command.Parameters.AddWithValue("tipo_sanguineo", formData.TipoSanguineo);
                command.Parameters.AddWithValue("rh", formData.Rh);
                command.Parameters.AddWithValue("situacao", formData.Situacao);
                command.Parameters.AddWithValue("codigo", formData.Codigo);

                Console.WriteLine($"SQL Query: {sql}");
                Console.WriteLine($"Parâmetros: {JsonSerializer.Serialize(new {
                    formData.Nome,
                    formData.Cpf,
                    formData.Contato,
                    formData.TipoSanguineo,
                    formData.Rh,
                    formData.Situacao,
                    formData.Codigo
                })}");

                await command.ExecuteNonQueryAsync();
            }
        }

        await httpContext.Response.WriteAsync("Dados do doador atualizados com sucesso.");
    }
    catch (Exception ex)
    {
        httpContext.Response.StatusCode = 500;
        await httpContext.Response.WriteAsync($"Erro ao processar a requisição: {ex.Message}");
    }
});


app.MapPost("/home", async (HttpContext httpContext) =>
{
    try
    {
        // Ler e desserializar dados do corpo da requisição
        var requestBody = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
        var formData = JsonSerializer.Deserialize<FormData>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Logar os dados recebidos
        Console.WriteLine("Dados recebidos:");
        Console.WriteLine(JsonSerializer.Serialize(formData));

        // Validar os dados do formulário
        if (string.IsNullOrEmpty(formData.Nome) ||
            string.IsNullOrEmpty(formData.Cpf) ||
            string.IsNullOrEmpty(formData.Contato) ||
            string.IsNullOrEmpty(formData.TipoSanguineo) ||
            string.IsNullOrEmpty(formData.Rh))
        {
            httpContext.Response.StatusCode = 400; // Bad Request
            await httpContext.Response.WriteAsync("Todos os campos obrigatórios devem ser preenchidos.");
            return;
        }

        // Conectar ao banco de dados PostgreSQL e inserir dados
        string connectionString = "Host=172.30.226.119;Port=5433;Username=postgres;Password=123456;Database=bancodb";

        using (var conn = new NpgsqlConnection(connectionString))
        {
            await conn.OpenAsync();

            using (var cmd = new NpgsqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = "INSERT INTO public.doador (nome, cpf, contato, tipo_sanguineo, rh, tipo_rh_corretos, situacao) VALUES (@nome, @cpf, @contato, @tipo_sanguineo, @rh, @tipo_rh_corretos, @situacao)";
                cmd.Parameters.AddWithValue("nome", formData.Nome);
                cmd.Parameters.AddWithValue("cpf", formData.Cpf);
                cmd.Parameters.AddWithValue("contato", formData.Contato);
                cmd.Parameters.AddWithValue("tipo_sanguineo", formData.TipoSanguineo);
                cmd.Parameters.AddWithValue("rh", formData.Rh);
                cmd.Parameters.AddWithValue("tipo_rh_corretos", formData.TipoRhCorretos); // Utilizando o valor padrão
                cmd.Parameters.AddWithValue("situacao", formData.Situacao);

                await cmd.ExecuteNonQueryAsync();
            }
        }

        // Se os dados forem válidos, retornar os dados recebidos
        await httpContext.Response.WriteAsJsonAsync(formData);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro ao processar a requisição: {ex.Message}");
        Console.WriteLine(ex.StackTrace);

        httpContext.Response.StatusCode = 500;
        await httpContext.Response.WriteAsync($"Erro ao processar a requisição: {ex.Message}");
    }
});


app.MapGet("/validar", async (HttpContext httpContext) =>
{

    try
    {
        string connectionString = "Host=172.30.226.119;Port=5433;Username=postgres;Password=123456;Database=bancodb";
        List<FormData> doadores = new List<FormData>();

        var queryParams = httpContext.Request.Query;
        string codigo = queryParams["codigo"];
        string nome = queryParams["nome"];
        string cpf = queryParams["cpf"];
        string contato = queryParams["contato"];
        string tipoSanguineo = queryParams["opcaoSelect"];
        string rh = queryParams["opcaoRadio"];
        //bool? booleano = queryParams.ContainsKey("booleano") ? (bool?)bool.Parse(queryParams["booleano"]) : null;

        // Log dos parâmetros recebidos

        using (var conn = new NpgsqlConnection(connectionString))
        {
            await conn.OpenAsync();

            using (var cmd = new NpgsqlCommand())
            {
                cmd.Connection = conn;

                var sql = "SELECT * FROM public.doador WHERE situacao = 'Ativo'";

                if (!string.IsNullOrEmpty(codigo) && codigo != "-1")
                {
                    sql += " AND codigo = @codigo";
                    cmd.Parameters.AddWithValue("codigo", long.Parse(codigo));
                }

                if (!string.IsNullOrEmpty(nome))
                {
                    sql += " AND nome ILIKE @nome";
                    cmd.Parameters.AddWithValue("nome", $"%{nome}%");
                }

                if (!string.IsNullOrEmpty(cpf))
                {
                    sql += " AND cpf = @cpf";
                    cmd.Parameters.AddWithValue("cpf", cpf);
                }

                if (!string.IsNullOrEmpty(contato))
                {
                    sql += " AND contato ILIKE @contato";
                    cmd.Parameters.AddWithValue("contato", $"%{contato}%");
                }

                if (!string.IsNullOrEmpty(tipoSanguineo))
                {
                    sql += " AND tipo_sanguineo = @tipoSanguineo";
                    cmd.Parameters.AddWithValue("tipoSanguineo", tipoSanguineo);
                }

                if (!string.IsNullOrEmpty(rh))
                {
                    sql += " AND rh = @rh";
                    cmd.Parameters.AddWithValue("rh", rh);
                }

                /*if (booleano.HasValue)
                {
                    sql += " AND tipo_rh_corretos = @booleano";
                    cmd.Parameters.AddWithValue("booleano", booleano.Value);
                }
                else
                {
                    // Se booleano não tiver valor, considerar tanto true quanto false
                    sql += " AND (tipo_rh_corretos = true OR tipo_rh_corretos = false)";
                }*/

                cmd.CommandText = sql;

                // Log da query SQL gerada
                Console.WriteLine($"Query SQL: {cmd.CommandText}");

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var formData = new FormData
                        {
                            Codigo = reader.GetInt64(0),
                            Nome = reader.GetString(1),
                            Cpf = reader.GetString(2),
                            Contato = reader.GetString(3),
                            TipoSanguineo = reader.GetString(4),
                            Rh = reader.GetString(5),
                            Situacao = reader.GetString(7)
                        };
                        doadores.Add(formData);
                    }
                }
            }
        }
        // Log da quantidade de doadores encontrados
        Console.WriteLine($"Doadores encontrados: {doadores.Count}");

        // Envio da resposta
        await httpContext.Response.WriteAsJsonAsync(doadores);
    }
    catch (Exception ex)
    {
        httpContext.Response.StatusCode = 500; // Internal Server Error
        await httpContext.Response.WriteAsync($"Erro ao buscar dados do banco de dados: {ex.Message}");
    }
});


app.MapGet("/doacoes", async (HttpContext httpContext) =>
{
    try
    {
        var queryParams = httpContext.Request.Query;
        string codigoDoador = queryParams["codigoDoador"];

        if (string.IsNullOrEmpty(codigoDoador))
        {
            httpContext.Response.StatusCode = 400; // Bad Request
            await httpContext.Response.WriteAsync("Código do doador é obrigatório.");
            return;
        }

        string connectionString = "Host=172.30.226.119;Port=5433;Username=postgres;Password=123456;Database=bancodb";
        List<object> doacoes = new List<object>();

        using (var conn = new NpgsqlConnection(connectionString))
        {
            await conn.OpenAsync();

            using (var cmd = new NpgsqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = "SELECT B.nome, B.tipo_sanguineo, A.data, A.hora, A.volume " +
                                  "FROM public.doacao AS A " + 
                                  "LEFT JOIN public.doador AS B " +
                                  "ON A.codigo_doador = B.codigo " +
                                  "WHERE B.codigo = @codigoDoador";
                cmd.Parameters.AddWithValue("codigoDoador", long.Parse(codigoDoador));

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var doacao = new
                        {
                            Nome = reader.GetString(0),
                            TipoSanguineo = reader.GetString(1),
                            Data = reader.GetDateTime(2).ToString("yyyy-MM-dd"),
                            Hora = reader.GetTimeSpan(3).ToString(@"hh\:mm"),
                            Volume = reader.GetDouble(4)
                        };
                        doacoes.Add(doacao);
                    }
                }
            }
        }

        // Log dos dados de doações
        Console.WriteLine($"Doações encontradas: {doacoes.Count}");
        await httpContext.Response.WriteAsJsonAsync(doacoes);
    }
    catch (Exception ex)
    {
        httpContext.Response.StatusCode = 500; // Internal Server Error
        await httpContext.Response.WriteAsync($"Erro ao buscar dados do banco de dados: {ex.Message}");
    }
});

app.MapPost("/doar-sangue", async (HttpContext httpContext) =>
{
    try
    {
        var requestBody = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new DateTimeConverter("yyyy-MM-dd"), new TimeSpanConverter(@"hh\:mm") }
        };

        var doacaoRequest = JsonSerializer.Deserialize<DoacaoRequest>(requestBody, options);

        // Convertendo a data e hora da coleta para DateTime e TimeSpan
        var dataColeta = DateTime.ParseExact(doacaoRequest.DataColeta, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var horaColeta = TimeSpan.ParseExact(doacaoRequest.HoraColeta, @"hh\:mm", CultureInfo.InvariantCulture);

        System.Console.WriteLine(dataColeta);

        // Validar se os dados recebidos são válidos
        if (doacaoRequest == null || doacaoRequest.CodigoDoador <= 0 || doacaoRequest.Volume <= 0)
        {
            httpContext.Response.StatusCode = 400;
            await httpContext.Response.WriteAsync("Dados inválidos.");
            return;
        }

        string connectionString = "Host=172.30.226.119;Port=5433;Username=postgres;Password=123456;Database=bancodb";

        using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();

            var sql = "INSERT INTO public.doacao (data, hora, volume, situacao, codigo_doador) " +
                      "VALUES (@data, @hora, @volume, @situacao, @codigo_doador)";

            using (var command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("data", dataColeta); // Usando a data convertida
                command.Parameters.AddWithValue("hora", horaColeta); // Usando a hora convertida
                command.Parameters.AddWithValue("volume", doacaoRequest.Volume);
                command.Parameters.AddWithValue("situacao", "Concluida");
                command.Parameters.AddWithValue("codigo_doador", doacaoRequest.CodigoDoador);

                var result = await command.ExecuteNonQueryAsync();
                Console.WriteLine($"Número de linhas afetadas: {result}");
            }

            // Atualizar o campo 'tipo_rh_corretos' na tabela 'doador'
            var sqlUpdateDoador = "UPDATE public.doador SET tipo_rh_corretos = true WHERE codigo = @codigo_doador";

            using (var commandUpdate = new NpgsqlCommand(sqlUpdateDoador, connection))
            {
                commandUpdate.Parameters.AddWithValue("codigo_doador", doacaoRequest.CodigoDoador);

                var resultUpdate = await commandUpdate.ExecuteNonQueryAsync();
                Console.WriteLine($"Número de linhas afetadas (UPDATE Doador): {resultUpdate}");
            }
        }

        httpContext.Response.StatusCode = 200;
        await httpContext.Response.WriteAsync("Doação registrada com sucesso.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro ao processar a requisição: {ex.Message}");
        httpContext.Response.StatusCode = 500;
        await httpContext.Response.WriteAsync($"Erro ao processar a requisição: {ex.Message}");
    }
});

app.MapGet("/search-donations", async (HttpContext httpContext) =>
{
    string startDate = httpContext.Request.Query["start_date"];
    string endDate = httpContext.Request.Query["end_date"];

    string connectionString = "Host=172.30.226.119;Port=5433;Username=postgres;Password=123456;Database=bancodb";

    var sql = @"
        SELECT d.codigo, 
               DATE(d.data) as data, 
               d.hora, 
               d.volume, 
               o.nome
        FROM public.doacao d
        JOIN public.doador o ON d.codigo_doador = o.codigo
        WHERE (@startDate IS NULL OR d.data >= @startDate)
          AND (@endDate IS NULL OR d.data <= @endDate)
    ";

    using (var connection = new NpgsqlConnection(connectionString))
    {
        await connection.OpenAsync();

        var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("startDate", string.IsNullOrEmpty(startDate) ? (object)DBNull.Value : DateTime.Parse(startDate));
        command.Parameters.AddWithValue("endDate", string.IsNullOrEmpty(endDate) ? (object)DBNull.Value : DateTime.Parse(endDate));

        var doacoes = new List<DonationData>();

        using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var doacao = new DonationData
                {
                    DonationCode = reader.GetInt32(reader.GetOrdinal("codigo")),
                    Date = reader.GetDateTime(reader.GetOrdinal("data")),
                    Time = reader.IsDBNull(reader.GetOrdinal("hora")) 
                           ? TimeSpan.Zero 
                           : reader.GetTimeSpan(reader.GetOrdinal("hora")), // Define um valor padrão se o campo for nulo
                    Volume = (double)reader.GetDecimal(reader.GetOrdinal("volume")),
                    DonorName = reader.GetString(reader.GetOrdinal("nome"))
                };

                doacoes.Add(doacao);
            }
        }

        var jsonResponse = JsonSerializer.Serialize(doacoes);
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsync(jsonResponse);
    }
});



app.Run();

public class DateTimeConverter : System.Text.Json.Serialization.JsonConverter<DateTime>
{
    private readonly string _format;

    public DateTimeConverter(string format)
    {
        _format = format;
    }

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string dateString = reader.GetString();
            if (DateTime.TryParseExact(dateString, _format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            {
                return result;
            }
        }

        // Se não conseguir converter, lançar exceção ou retornar DateTime.MinValue
        return DateTime.MinValue;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(_format));
    }
}

public class TimeSpanConverter : System.Text.Json.Serialization.JsonConverter<TimeSpan>
{
    private readonly string _format;

    public TimeSpanConverter(string format)
    {
        _format = format;
    }

    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string timeString = reader.GetString();
            if (TimeSpan.TryParseExact(timeString, _format, CultureInfo.InvariantCulture, out TimeSpan result))
            {
                return result;
            }
        }

        // Se não conseguir converter, lançar exceção ou retornar TimeSpan.Zero
        return TimeSpan.Zero;
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(_format));
    }
}

public class DoacaoRequest
{
    public long CodigoDoador { get; set; }
    public string DataColeta { get; set; } // Alterado para string
    public string HoraColeta { get; set; } // Alterado para string
    public double Volume { get; set; }
}

public class Doacao
{
    public long CodigoDoador { get; set; }
    public DateTime Data { get; set; }
    public TimeSpan Hora { get; set; }
    public double Volume { get; set; }

}


// Classe para representar os dados do formulário
public class FormData
{
    public long Codigo { get; set; }
    public string Nome { get; set; }
    public string Cpf { get; set; }
    public string Contato { get; set; }
    public string TipoSanguineo { get; set; }
    public string Rh { get; set; }
    public bool TipoRhCorretos {get; set; }
    public string Situacao { get; set; }

    public FormData()
    {
        TipoRhCorretos = false; // Valor padrão
    }
}

public class DonationData
{
    public long DonationCode { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public double Volume { get; set; }
    public string DonorName { get; set; }
}
