using CS_mongoDB.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CS_mongoDB
{
     public class BooksService
     {
          private readonly IMongoCollection<Book> _booksCollection;

          private RSAParameters _privateKey;
          private RSAParameters _publicKey;

          public BooksService(
              IOptions<BookStoreDatabaseSettings> bookStoreDatabaseSettings)
          {
               var mongoClient = new MongoClient(
                   bookStoreDatabaseSettings.Value.ConnectionString);

               var mongoDatabase = mongoClient.GetDatabase(
                   bookStoreDatabaseSettings.Value.DatabaseName);

               _booksCollection = mongoDatabase.GetCollection<Book>(
                   bookStoreDatabaseSettings.Value.BooksCollectionName);

               var cryptoServiceProvider = new RSACryptoServiceProvider(2048); //2048 - Długość klucza
               _privateKey = cryptoServiceProvider.ExportParameters(true); //Generowanie klucza prywatnego
               _publicKey = cryptoServiceProvider.ExportParameters(false); //Generowanie klucza publiczny
          }

          public async Task<List<Book>> GetAsync() =>
              await _booksCollection.Find(_ => true).ToListAsync();

          public async Task<List<Book>> GetDecryptedAsync()
          {
               var books = await _booksCollection.Find(_ => true).ToListAsync();
               foreach (var book in books)
               {
                    Decrypt(book);
               }
               return books;
          }

          public async Task<Book?> GetAsync(string id) =>
              await _booksCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

          public async Task CreateAsync(Book newBook)
          {
               Encript(newBook);
               await _booksCollection.InsertOneAsync(newBook);
          }

          public async Task UpdateAsync(string id, Book updatedBook) =>
              await _booksCollection.ReplaceOneAsync(x => x.Id == id, updatedBook);

          public async Task RemoveAsync(string id) =>
              await _booksCollection.DeleteOneAsync(x => x.Id == id);


          public void Encript(Book book)
          {
               string publicKeyString = GetKeyString(_publicKey);
               book.BookName = this.EncryptField(book.BookName, publicKeyString);
               book.Category = this.EncryptField(book.Category, publicKeyString);
               book.Author = this.EncryptField(book.Author, publicKeyString);
          }

          public void Decrypt(Book book)
          {
               string privateKeyString = GetKeyString(_privateKey);
               book.BookName = DecryptField(book.BookName, privateKeyString);
               book.Category = DecryptField(book.Category, privateKeyString);
               book.Author = DecryptField(book.Author, privateKeyString);
          }

          private string EncryptField(string textToEncrypt, string publicKeyString)
          {
               var bytesToEncrypt = Encoding.UTF8.GetBytes(textToEncrypt);

               using var rsa = new RSACryptoServiceProvider(2048);
               try
               {
                    rsa.FromXmlString(publicKeyString.ToString());
                    var encryptedData = rsa.Encrypt(bytesToEncrypt, true);
                    var base64Encrypted = Convert.ToBase64String(encryptedData);
                    return base64Encrypted;
               }
               finally
               {
                    rsa.PersistKeyInCsp = false;
               }
          }

          private string DecryptField(string textToDecrypt, string privateKeyString)
          {
               using var rsa = new RSACryptoServiceProvider(2048);
               try
               {
                    // server decrypting data with private key                    
                    rsa.FromXmlString(privateKeyString);

                    var resultBytes = Convert.FromBase64String(textToDecrypt);
                    var decryptedBytes = rsa.Decrypt(resultBytes, true);
                    var decryptedData = Encoding.UTF8.GetString(decryptedBytes);
                    return decryptedData.ToString();
               }
               finally
               {
                    rsa.PersistKeyInCsp = false;
               }
          }
          private static string GetKeyString(RSAParameters publicKey)
          {
               var stringWriter = new System.IO.StringWriter();
               var xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
               xmlSerializer.Serialize(stringWriter, publicKey);
               return stringWriter.ToString();
          }
     }
}
