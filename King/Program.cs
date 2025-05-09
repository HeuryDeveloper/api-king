using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// Configura��es para JSON
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var app = builder.Build();

//Usar vari�veis de ambiente para obter os dados da conex�o
string dbHost = Environment.GetEnvironmentVariable("MYSQLHOST");
string dbPort = Environment.GetEnvironmentVariable("MYSQLPORT");
string dbUser = Environment.GetEnvironmentVariable("MYSQLUSER");
string dbPassword = Environment.GetEnvironmentVariable("MYSQLPASSWORD");
string dbName = Environment.GetEnvironmentVariable("MYSQLDATABASE");

//string connectionString = $"Server={dbHost};Port={dbPort};Database={dbName};Uid={dbUser};Pwd={dbPassword};";
var connectionString = builder.Configuration.GetValue<string>("CONNECTION_STRING");
//string connectionString = "Server=localhost;Port=3306;Database=king;User=root;Password=1234";
//string connectionString = "Server=hopper.proxy.rlwy.net;Port=10728;Database=railway;Uid=root;Pwd=ZsJZKjXPpZmTBiNtXTcHEJFNDHnvOHNk;";

// ============================
// INSERIR PRODUTO
// ============================
app.MapPost("/inserir-produto", async (Produto produto) =>
{
    Console.WriteLine($"Recebido: {JsonSerializer.Serialize(produto)}");

    using var conn = new MySqlConnection(connectionString);
    await conn.OpenAsync();

    //string apagar = "DESCRIBE produtos";
    //using var cmd1 = new MySqlCommand(apagar, conn);
    //await cmd1.ExecuteNonQueryAsync();

    string query = @"INSERT INTO produtos 
                    (nome, descricao, codigo_fornecedor, preco_venda, preco_compra, quantidade_estoque)
                    VALUES (@nome, @descricao, @codigo_fornecedor, @preco_venda, @preco_compra, @quantidade_estoque)";

    using var cmd = new MySqlCommand(query, conn);
    cmd.Parameters.AddWithValue("@nome", produto.Nome);
    cmd.Parameters.AddWithValue("@descricao", produto.Descricao);
    cmd.Parameters.AddWithValue("@codigo_fornecedor", produto.CodigoFornecedor);
    cmd.Parameters.AddWithValue("@preco_venda", produto.PrecoVenda);
    cmd.Parameters.AddWithValue("@preco_compra", produto.PrecoCompra);
    cmd.Parameters.AddWithValue("@quantidade_estoque", produto.QuantidadeEstoque);

    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { mensagem = "Produto inserido com sucesso." });
});

// ==============
// INSERIR COMPRA
// ==============
app.MapPost("/inserir-compra", async (Compra compra) =>
{
    using var conn = new MySqlConnection(connectionString);
    await conn.OpenAsync();

    string query = @"INSERT INTO compras 
                    (data_compra, fornecedor, valor_total, num_pedido, idPagamento)
                    VALUES (@dataCompra, @fornecedor, @valorTotal, @pedido, @idPagamento)";

    using var cmd = new MySqlCommand(query, conn);
    cmd.Parameters.AddWithValue("@dataCompra", compra.DataCompra);
    cmd.Parameters.AddWithValue("@fornecedor", compra.Fornecedor);
    cmd.Parameters.AddWithValue("@valorTotal", compra.ValorTotal);
    cmd.Parameters.AddWithValue("@pedido", compra.Pedido);
    cmd.Parameters.AddWithValue("@idPagamento", compra.IDPagamento);

    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { mensagem = "Nova compra inserida com sucesso." });
});

// =======================
// INSERIR ITENS DA COMPRA
// =======================


app.MapPost("/criar-tabela", async (Produto produto) =>
{
    string connectionString1 = "Server=hopper.proxy.rlwy.net;Port=10728;Database=railway;Uid=root;Pwd=ZsJZKjXPpZmTBiNtXTcHEJFNDHnvOHNk;";
    using var conn = new MySqlConnection(connectionString1);
    await conn.OpenAsync();

    string criar = "CREATE TABLE `itens_compra` (`id` int(11) NOT NULL AUTO_INCREMENT, `compra_id` int(11) DEFAULT NULL, `produto_id` int(11) DEFAULT NULL, `quantidade` int(11) DEFAULT NULL, `preco_unitario` decimal(10,2) DEFAULT NULL, PRIMARY KEY (`id`), KEY `compra_id` (`compra_id`), KEY `produto_id` (`produto_id`), CONSTRAINT `itens_compra_ibfk_1` FOREIGN KEY (`compra_id`) REFERENCES `compras` (`id`), CONSTRAINT `itens_compra_ibfk_2` FOREIGN KEY (`produto_id`) REFERENCES `produtos` (`id`)) ENGINE=InnoDB DEFAULT CHARSET=utf8;";
    using var cmd1 = new MySqlCommand(criar, conn);
    await cmd1.ExecuteNonQueryAsync();

    return Results.Ok(new { mensagem = "Tabela criada com sucesso." });
});

// ============================
// CONSULTAR PRODUTO
// ============================
app.MapGet("/consultar-produto/{nome}", async (string nome) =>
{
    using var conn = new MySqlConnection(connectionString);
    await conn.OpenAsync();

    string query = "SELECT * FROM produtos WHERE nome = @nome LIMIT 1";

    using var cmd = new MySqlCommand(query, conn);
    cmd.Parameters.AddWithValue("@nome", nome);

    using var reader = await cmd.ExecuteReaderAsync();

    if (await reader.ReadAsync())
    {
        var produto = new Produto
        {
            Nome = reader["nome"].ToString(),
            CodigoFornecedor = reader["codigo_fornecedor"].ToString(),
            PrecoVenda = Convert.ToDecimal(reader["preco_venda"]),
            PrecoCompra = Convert.ToDecimal(reader["preco_compra"]),
            QuantidadeEstoque = Convert.ToInt32(reader["quantidade_estoque"])
        };

        return Results.Ok(produto);
    }

    return Results.NotFound(new { mensagem = "Produto n�o encontrado." });
});

// ============================
// INSERIR CLIENTE
// ============================
app.MapPost("/inserir-cliente", async (Cliente cliente) =>
{
    using var conn = new MySqlConnection(connectionString);
    await conn.OpenAsync();

    string query = @"INSERT INTO clientes (nome, cpf, email, telefone, endereco)
                     VALUES (@nome, @cpf, @email, @telefone, @endereco)";

    using var cmd = new MySqlCommand(query, conn);
    cmd.Parameters.AddWithValue("@nome", cliente.Nome);
    cmd.Parameters.AddWithValue("@cpf", cliente.CPF);
    cmd.Parameters.AddWithValue("@email", cliente.Email);
    cmd.Parameters.AddWithValue("@telefone", cliente.Telefone);
    cmd.Parameters.AddWithValue("@endereco", cliente.Endereco);

    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { mensagem = "Cliente inserido com sucesso." });
});

// ============================
// INSERIR VENDA
// ============================
app.MapPost("/inserir-venda", async (Venda venda) =>
{
    using var conn = new MySqlConnection(connectionString);
    await conn.OpenAsync();

    MySqlTransaction transaction = await conn.BeginTransactionAsync();

    try
    {
        // 1. Inserir na tabela vendas
        string vendaQuery = @"INSERT INTO vendas (data_venda, cliente_id, valor_total)
                              VALUES (NOW(), @cliente_id, @valor_total)";
        using var cmdVenda = new MySqlCommand(vendaQuery, conn, transaction);
        cmdVenda.Parameters.AddWithValue("@cliente_id", venda.ClienteId);
        cmdVenda.Parameters.AddWithValue("@valor_total", venda.ValorTotal);
        await cmdVenda.ExecuteNonQueryAsync();

        int vendaId = (int)cmdVenda.LastInsertedId;

        // 2. Inserir cada item da venda
        foreach (var item in venda.Itens)
        {
            string itemQuery = @"INSERT INTO itens_venda (venda_id, produto_id, quantidade, preco_unitario)
                                 VALUES (@venda_id, @produto_id, @quantidade, @preco_unitario)";
            using var cmdItem = new MySqlCommand(itemQuery, conn, transaction);
            cmdItem.Parameters.AddWithValue("@venda_id", vendaId);
            cmdItem.Parameters.AddWithValue("@produto_id", item.ProdutoId);
            cmdItem.Parameters.AddWithValue("@quantidade", item.Quantidade);
            cmdItem.Parameters.AddWithValue("@preco_unitario", item.PrecoUnitario);
            await cmdItem.ExecuteNonQueryAsync();

            // Atualizar estoque
            string updateEstoque = @"UPDATE produtos SET quantidade_estoque = quantidade_estoque - @quantidade
                                     WHERE id = @produto_id";
            using var cmdEstoque = new MySqlCommand(updateEstoque, conn, transaction);
            cmdEstoque.Parameters.AddWithValue("@quantidade", item.Quantidade);
            cmdEstoque.Parameters.AddWithValue("@produto_id", item.ProdutoId);
            await cmdEstoque.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
        return Results.Ok(new { mensagem = "Venda registrada com sucesso." });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        return Results.Problem("Erro ao registrar venda: " + ex.Message);
    }
});

// ===================================================
// CONSULTAR ESTOQUE DE UM PRODUTO POR NOME PELA ALEXA
// ===================================================
app.MapPost("/alexa/verificar-estoque", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();

    var requestData = JsonSerializer.Deserialize<AlexaRequest>(body, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });

    string nomeProduto = requestData?.Request?.Intent?.Slots?["nomeProduto"]?.Value ?? "";

    Console.WriteLine($"Produto recebido da Alexa: {nomeProduto}");

    if (string.IsNullOrWhiteSpace(nomeProduto))
    {
        return Results.BadRequest(new
        {
            response = new
            {
                outputSpeech = new
                {
                    type = "PlainText",
                    text = "Desculpe, n�o consegui entender o nome do produto."
                },
                shouldEndSession = true
            }
        });
    }

    var produto = await ObterProdutoPorNome(nomeProduto);

    string respostaAlexa = produto != null
        ? $"Voce tem {produto.QuantidadeEstoque} unidades do produto {produto.Nome}."
        : $"N�o encontrei o produto {nomeProduto} no estoque.";

    return Results.Json(new
    {
        response = new
        {
            outputSpeech = new
            {
                type = "PlainText",
                text = respostaAlexa
            },
            shouldEndSession = true
        }
    });
});

// =====================================================
// CONSULTAR PRODUTO POR CODIGO DO FORNECEDOR PELA ALEXA
// =====================================================
app.MapPost("/alexa/buscar-produto", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();

    var dados = JsonSerializer.Deserialize<Dictionary<string, string>>(body);

    dados.TryGetValue("informacao", out var informacao);
    dados.TryGetValue("codigo", out var codigo); // Pode ser null, se n�o enviado

    var requestData = JsonSerializer.Deserialize<AlexaRequest>(body, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });

    string campo = informacao;
    string codProduto = Regex.Match(campo, @"\d+").Value;
    bool naoFalarNomeProduto = false;

    if (string.IsNullOrWhiteSpace(codProduto))
    {
        codProduto = codigo;
        naoFalarNomeProduto = true;
    }

    if (string.IsNullOrWhiteSpace(codProduto))
    {
        return Results.BadRequest(new
        {
            response = new
            {
                outputSpeech = new
                {
                    type = "PlainText",
                    text = "Desculpe, n�o consegui entender o que voce disse."
                }
                //shouldEndSession = true
            }
        });
    }

    var produto = await ObterProdutoPorCodigo(codProduto);
    string respostaAlexa = "";

    if (campo.Contains("estoque") || campo.Contains("quantidade"))
    {
        if (naoFalarNomeProduto == false)
        {
            respostaAlexa = produto != null
                ? $"Voce tem {produto.QuantidadeEstoque} unidades do produto {produto.Descricao}."
                : $"Nao encontrei o produto {codProduto} no estoque.";
        }
        else
        {
            respostaAlexa = produto != null
                ? $"Voce tem {produto.QuantidadeEstoque} unidades desse produto."
                : $"Nao encontrei o produto {codProduto} no estoque.";
        }
    }
    else if (campo.Contains("compra") || campo.Contains("comprei") || campo.Contains("paguei") || campo.Contains("pago") || campo.Contains("custou"))
    {
        if (naoFalarNomeProduto == false)
        {
            respostaAlexa = produto != null
                ? $"Voce comprou o produto {produto.Descricao} por {produto.PrecoCompra}."
                : $"Nao encontrei o produto {codProduto} no cadastro.";
        }
        else
        {
            respostaAlexa = produto != null
                ? $"Voce comprou este produto por {produto.PrecoCompra}."
                : $"Nao encontrei o produto {codProduto} no cadastro.";
        }
    }
    else if (campo.Contains("venda") || campo.Contains("vendo") || campo.Contains("vendendo") || campo.Contains("custa"))
    {
        if (naoFalarNomeProduto == false)
        {
            respostaAlexa = produto != null
                ? $"O valor de venda do produto {produto.Descricao} e de {produto.PrecoVenda}."
                : $"Nao encontrei o produto {codProduto} no cadastro.";
        }
        else
        {
            respostaAlexa = produto != null
                ? $"O valor de venda deste produto e de {produto.PrecoVenda}."
                : $"Nao encontrei o produto {codProduto} no cadastro.";
        }
    }
    else if (campo.Contains("descri") || campo.Contains("nome"))
    {
        respostaAlexa = produto != null
            ? $"A descricao do produto {produto.CodigoFornecedor} e {produto.Descricao}."
            : $"Nao encontrei o produto {codProduto} no cadastro.";
    }
    else if (campo.Contains("lucro") || campo.Contains("ganhando"))
    {
        if (naoFalarNomeProduto == false)
        {
            respostaAlexa = produto != null
                ? $"O lucro do produto {produto.Descricao} e de {produto.PrecoVenda - produto.PrecoCompra} que representa {Convert.ToDouble((produto.PrecoVenda - produto.PrecoCompra) / produto.PrecoCompra * 100).ToString("0.00")} porcento."
                : $"Nao encontrei o produto {codProduto} no cadastro.";
        }
        else
        {
            respostaAlexa = produto != null
                ? $"O lucro deste produto e de {produto.PrecoVenda - produto.PrecoCompra} que representa {Convert.ToDouble((produto.PrecoVenda - produto.PrecoCompra) / produto.PrecoCompra * 100).ToString("0.00")} porcento."
                : $"Nao encontrei o produto {codProduto} no cadastro.";
        }
    }

    return Results.Json(new
    {
        mensagem = respostaAlexa,
        codigo = codProduto
    });
});


// Fun��o para buscar produto no banco de dados
async Task<Produto?> ObterProdutoPorCodigo(string codigo)
{
    //string connectionString = "Server=localhost;Port=3306;Database=king;User=root;Password=1234";
    string connectionString = "Server=hopper.proxy.rlwy.net;Port=10728;Database=railway;Uid=root;Pwd=ZsJZKjXPpZmTBiNtXTcHEJFNDHnvOHNk;";

    using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();

    var cmd = new MySqlCommand("SELECT id, nome, descricao, codigo_fornecedor, preco_venda, preco_compra, quantidade_estoque FROM produtos WHERE codigo_fornecedor = @codigo", connection);
    cmd.Parameters.AddWithValue("@codigo", codigo);

    using var reader = await cmd.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        return new Produto
        {
            Id = reader.GetInt32(0),
            Nome = reader.GetString(1),
            Descricao = reader.GetString(2),
            CodigoFornecedor = reader.GetString(3),
            PrecoVenda = reader.GetDecimal(4),
            PrecoCompra = reader.GetDecimal(5),
            QuantidadeEstoque = reader.GetInt32(6)
        };
    }

    return null;
}

// Fun��o para buscar produto no banco de dados
async Task<Produto?> ObterProdutoPorNome(string nome)
{
    //string connectionString = "Server=localhost;Port=3306;Database=king;User=root;Password=1234";
    string connectionString = "Server=hopper.proxy.rlwy.net;Port=10728;Database=railway;Uid=root;Pwd=ZsJZKjXPpZmTBiNtXTcHEJFNDHnvOHNk;";

    using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();

    var cmd = new MySqlCommand("SELECT id, nome, descricao, codigo_fornecedor, preco_venda, preco_compra, quantidade_estoque FROM produtos WHERE nome = @nome", connection);
    cmd.Parameters.AddWithValue("@nome", nome);

    using var reader = await cmd.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        return new Produto
        {
            Id = reader.GetInt32(0),
            Nome = reader.GetString(1),
            Descricao = reader.GetString(2),
            CodigoFornecedor = reader.GetString(3),
            PrecoVenda = reader.GetDecimal(4),
            PrecoCompra = reader.GetDecimal(5),
            QuantidadeEstoque = reader.GetInt32(6)
        };
    }

    return null;
}

app.Run();


// ============================
// MODELOS DE DADOS
// ============================
public class Produto
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Descricao { get; set; }
    public string CodigoFornecedor { get; set; }
    public decimal PrecoVenda { get; set; }
    public decimal PrecoCompra { get; set; }
    public int QuantidadeEstoque { get; set; }
}

public class Cliente
{
    public string Nome { get; set; }
    public string CPF { get; set; }
    public string Email { get; set; }
    public string Telefone { get; set; }
    public string Endereco { get; set; }
}

public class Compra
{
    public int ID { get; set; }
    public DateTime DataCompra { get; set; }
    public string Fornecedor { get; set; }
    public decimal ValorTotal { get; set; }
    public string Pedido { get; set; }
    public int IDPagamento { get; set; }
    public List<ItensCompra> Itens { get; set; }
}

public class ItensCompra
{
    public int ID { get; set; }
    public int IDCompra { get; set; }
    public int IDProduto { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}

public class Venda
{
    public int ClienteId { get; set; }
    public decimal ValorTotal { get; set; }
    public List<ItemVenda> Itens { get; set; }
}

public class ItemVenda
{
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}

public class AlexaRequest
{
    public Request Request { get; set; }
}

public class Request
{
    public Intent Intent { get; set; }
}

public class Intent
{
    public Dictionary<string, Slot> Slots { get; set; }
}

public class Slot
{
    public string Name { get; set; }
    public string Value { get; set; }
}
