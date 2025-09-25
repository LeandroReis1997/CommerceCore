using CommerceCore.Domain.Common;
using System.Text.RegularExpressions;

namespace CommerceCore.Domain.Entities
{
    public class Address : BaseEntity
    {
        #region Constants

        private const int MIN_FIELD_LENGTH = 2;
        private const int MAX_STREET_LENGTH = 200;
        private const int MAX_NUMBER_LENGTH = 20;
        private const int MAX_CITY_LENGTH = 100;
        private const int MAX_STATE_LENGTH = 100;
        private const int ZIPCODE_LENGTH = 8; // CEP brasileiro sem hífen
        private const string ZIPCODE_PATTERN = @"^\d{8}$"; // 8 dígitos

        #endregion

        #region Properties

        // ID do usuário dono do endereço
        public Guid UserId { get; private set; }

        // Nome da rua/avenida
        public string Street { get; private set; } = string.Empty;

        // Número do endereço
        public string Number { get; private set; } = string.Empty;

        // Cidade
        public string City { get; private set; } = string.Empty;

        // Estado/UF
        public string State { get; private set; } = string.Empty;

        // CEP (sem hífen, 8 dígitos)
        public string ZipCode { get; private set; } = string.Empty;

        // Define se é o endereço padrão do usuário
        public bool IsDefault { get; private set; }

        #endregion

        #region Navigation Properties

        // Usuário dono do endereço
        public User User { get; set; } = null!;

        #endregion

        #region Constructors

        // Construtor vazio para ORMs (Dapper) e serialização JSON
        private Address() { }

        // Cria um novo endereço para o usuário
        public Address(Guid userId, string street, string number, string city, string state, string zipCode, bool isDefault = false)
        {
            ValidateAddressCreation(userId, street, number, city, state, zipCode); // Valida todos os dados
            SetAddressProperties(userId, street, number, city, state, zipCode, isDefault); // Define propriedades
        }

        #endregion

        #region Public Methods

        // Atualiza todos os dados do endereço
        public void UpdateAddress(string street, string number, string city, string state, string zipCode)
        {
            ValidateAddressFields(street, number, city, state, zipCode); // Valida novos dados
            SetAddressFields(street, number, city, state, zipCode);      // Define novos dados
            MarkAsUpdated();                                             // Marca como alterado
        }

        // Atualiza apenas a rua e número
        public void UpdateStreetInfo(string street, string number)
        {
            ValidateStreet(street);     // Valida rua
            ValidateNumber(number);     // Valida número
            SetStreet(street);         // Define nova rua
            SetNumber(number);         // Define novo número
            MarkAsUpdated();           // Marca como alterado
        }

        // Atualiza apenas cidade e estado
        public void UpdateCityState(string city, string state)
        {
            ValidateCity(city);        // Valida cidade
            ValidateState(state);      // Valida estado
            SetCity(city);            // Define nova cidade
            SetState(state);          // Define novo estado
            MarkAsUpdated();          // Marca como alterado
        }

        // Atualiza apenas o CEP
        public void UpdateZipCode(string zipCode)
        {
            ValidateZipCode(zipCode);  // Valida CEP
            SetZipCode(zipCode);      // Define novo CEP
            MarkAsUpdated();          // Marca como alterado
        }

        // Define este endereço como padrão
        public void SetAsDefault()
        {
            IsDefault = true;
            MarkAsUpdated();
        }

        // Remove este endereço como padrão
        public void UnsetAsDefault()
        {
            IsDefault = false;
            MarkAsUpdated();
        }

        // Query Methods - Métodos para consultar informações do endereço

        // Retorna endereço completo formatado
        public string GetFullAddress() =>
            $"{Street}, {Number} - {City}/{State} - CEP: {FormatZipCode()}";

        // Retorna endereço resumido (rua e cidade)
        public string GetShortAddress() =>
            $"{Street}, {Number} - {City}/{State}";

        // Retorna CEP formatado com hífen
        public string GetFormattedZipCode() => FormatZipCode();

        #endregion

        #region Private Helper Methods - Métodos auxiliares internos

        // Define todas as propriedades durante criação
        private void SetAddressProperties(Guid userId, string street, string number, string city, string state, string zipCode, bool isDefault)
        {
            UserId = userId;                              // Define usuário
            SetAddressFields(street, number, city, state, zipCode); // Define campos do endereço
            IsDefault = isDefault;                        // Define se é padrão
        }

        // Define os campos principais do endereço
        private void SetAddressFields(string street, string number, string city, string state, string zipCode)
        {
            SetStreet(street);         // Define rua
            SetNumber(number);         // Define número
            SetCity(city);            // Define cidade
            SetState(state);          // Define estado
            SetZipCode(zipCode);      // Define CEP
        }

        // Define rua (normalizada)
        private void SetStreet(string street)
        {
            Street = NormalizeField(street);
        }

        // Define número (normalizado)
        private void SetNumber(string number)
        {
            Number = NormalizeField(number);
        }

        // Define cidade (normalizada)
        private void SetCity(string city)
        {
            City = NormalizeField(city);
        }

        // Define estado (normalizado)
        private void SetState(string state)
        {
            State = NormalizeField(state);
        }

        // Define CEP (normalizado - só números)
        private void SetZipCode(string zipCode)
        {
            ZipCode = NormalizeZipCode(zipCode);
        }

        // Marca endereço como atualizado (para auditoria)
        private void MarkAsUpdated() => SetUpdatedAt();

        // Normaliza campo removendo espaços extras
        private static string NormalizeField(string field) => field.Trim();

        // Normaliza CEP removendo hífen e espaços
        private static string NormalizeZipCode(string zipCode) =>
            zipCode.Trim().Replace("-", "").Replace(" ", "");

        // Formata CEP com hífen (12345678 → 12345-678)
        private string FormatZipCode() =>
            ZipCode.Length == ZIPCODE_LENGTH ?
            $"{ZipCode.Substring(0, 5)}-{ZipCode.Substring(5)}" : ZipCode;

        #endregion

        #region Validation Methods - Métodos de validação

        // Valida todos os dados durante criação do endereço
        private static void ValidateAddressCreation(Guid userId, string street, string number, string city, string state, string zipCode)
        {
            ValidateUserId(userId);                                      // Valida UserId
            ValidateAddressFields(street, number, city, state, zipCode); // Valida campos do endereço
        }

        // Valida todos os campos do endereço
        private static void ValidateAddressFields(string street, string number, string city, string state, string zipCode)
        {
            ValidateStreet(street);     // Valida rua
            ValidateNumber(number);     // Valida número
            ValidateCity(city);        // Valida cidade
            ValidateState(state);      // Valida estado
            ValidateZipCode(zipCode);  // Valida CEP
        }

        // Valida se UserId é válido
        private static void ValidateUserId(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId é obrigatório", nameof(userId));
        }

        // Valida rua
        private static void ValidateStreet(string street)
        {
            ValidateFieldNotEmpty(street, "Rua", nameof(street));
            ValidateFieldLength(street, "Rua", MAX_STREET_LENGTH, nameof(street));
        }

        // Valida número
        private static void ValidateNumber(string number)
        {
            ValidateFieldNotEmpty(number, "Número", nameof(number));
            ValidateFieldLength(number, "Número", MAX_NUMBER_LENGTH, nameof(number));
        }

        // Valida cidade
        private static void ValidateCity(string city)
        {
            ValidateFieldNotEmpty(city, "Cidade", nameof(city));
            ValidateFieldLength(city, "Cidade", MAX_CITY_LENGTH, nameof(city));
        }

        // Valida estado
        private static void ValidateState(string state)
        {
            ValidateFieldNotEmpty(state, "Estado", nameof(state));
            ValidateFieldLength(state, "Estado", MAX_STATE_LENGTH, nameof(state));
        }

        // Valida CEP
        private static void ValidateZipCode(string zipCode)
        {
            ValidateFieldNotEmpty(zipCode, "CEP", nameof(zipCode));
            var normalizedZipCode = NormalizeZipCode(zipCode);

            if (normalizedZipCode.Length != ZIPCODE_LENGTH)
                throw new ArgumentException($"CEP deve ter {ZIPCODE_LENGTH} dígitos", nameof(zipCode));

            if (!Regex.IsMatch(normalizedZipCode, ZIPCODE_PATTERN))
                throw new ArgumentException("CEP deve conter apenas números", nameof(zipCode));
        }

        // Valida se campo não está vazio
        private static void ValidateFieldNotEmpty(string field, string fieldName, string paramName)
        {
            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException($"{fieldName} é obrigatório", paramName);

            if (field.Trim().Length < MIN_FIELD_LENGTH)
                throw new ArgumentException($"{fieldName} deve ter pelo menos {MIN_FIELD_LENGTH} caracteres", paramName);
        }

        // Valida tamanho do campo
        private static void ValidateFieldLength(string field, string fieldName, int maxLength, string paramName)
        {
            if (field.Trim().Length > maxLength)
                throw new ArgumentException($"{fieldName} não pode exceder {maxLength} caracteres", paramName);
        }

        #endregion
    }
}
