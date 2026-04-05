<?php

declare(strict_types=1);

use Greenter\Model\Client\Client;
use Greenter\Model\Company\Address;
use Greenter\Model\Company\Company;
use Greenter\Model\Sale\FormaPagos\FormaPagoContado;
use Greenter\Model\Sale\Invoice;
use Greenter\Model\Sale\Legend;
use Greenter\Model\Sale\SaleDetail;
use Greenter\See;
use Greenter\Ws\Services\ExtService;
use Greenter\Ws\Services\SoapClient;
use Greenter\Ws\Services\SunatEndpoints;

require __DIR__ . '/vendor/autoload.php';

const TEST_CERTIFICATE_PATH = __DIR__ . '/certificate.pem';

function readInput(): array
{
    $raw = stream_get_contents(STDIN);
    if ($raw === false || trim($raw) === '') {
        respond([
            'success' => false,
            'message' => 'No se recibió contenido JSON.',
        ], 1);
    }

    $data = json_decode($raw, true);
    if (!is_array($data)) {
        respond([
            'success' => false,
            'message' => 'El JSON recibido no es válido.',
        ], 1);
    }

    return $data;
}

function respond(array $data, int $exitCode = 0): void
{
    echo json_encode($data, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
    exit($exitCode);
}

function getValue(array $data, string $key, string $fallback = ''): string
{
    $value = $data[$key] ?? $fallback;
    return is_string($value) ? trim($value) : $fallback;
}

function createSee(string $ruc, string $user, string $password): See
{
    if (!file_exists(TEST_CERTIFICATE_PATH)) {
        throw new RuntimeException('No existe el certificado beta de pruebas.');
    }

    $see = new See();
    $see->setCertificate(file_get_contents(TEST_CERTIFICATE_PATH));
    $see->setService(SunatEndpoints::FE_BETA);
    $see->setClaveSOL($ruc, $user, $password);

    return $see;
}

function validateCredentials(string $ruc, string $user, string $password): array
{
    $soap = new SoapClient();
    $soap->setService(SunatEndpoints::FE_BETA);
    $soap->setCredentials($ruc . $user, $password);

    $service = new ExtService();
    $service->setClient($soap);

    $status = $service->getStatus('1');

    if (!$status->isSuccess()) {
        $error = $status->getError();
        $message = $error ? ($error->getCode() . ' - ' . $error->getMessage()) : 'No fue posible validar las credenciales SOL.';

        return [
            'success' => false,
            'message' => $message,
        ];
    }

    return [
        'success' => true,
        'message' => 'Credenciales SOL válidas para entorno beta.',
    ];
}

function buildInvoice(array $payload): Invoice
{
    $emisor = $payload['emisor'] ?? [];
    $cliente = $payload['cliente'] ?? [];
    $detalle = $payload['detalle'] ?? [];
    $totales = $payload['totales'] ?? [];

    $address = (new Address())
        ->setUbigueo(getValue($emisor, 'ubigeo', '150101'))
        ->setDepartamento(getValue($emisor, 'departamento', 'LIMA'))
        ->setProvincia(getValue($emisor, 'provincia', 'LIMA'))
        ->setDistrito(getValue($emisor, 'distrito', 'LIMA'))
        ->setUrbanizacion(getValue($emisor, 'urbanizacion', '-'))
        ->setDireccion(getValue($emisor, 'direccion', 'Av. Prueba 123'))
        ->setCodLocal(getValue($emisor, 'codLocal', '0000'));

    $company = (new Company())
        ->setRuc(getValue($emisor, 'ruc', '20000000001'))
        ->setRazonSocial(getValue($emisor, 'razonSocial', 'EMPRESA BETA SAC'))
        ->setNombreComercial(getValue($emisor, 'nombreComercial', getValue($emisor, 'razonSocial', 'EMPRESA BETA SAC')))
        ->setAddress($address);

    $client = (new Client())
        ->setTipoDoc(getValue($cliente, 'tipoDoc', '6'))
        ->setNumDoc(getValue($cliente, 'numDoc', '20000000001'))
        ->setRznSocial(getValue($cliente, 'razonSocial', 'CLIENTE BETA SAC'));

    $invoice = (new Invoice())
        ->setUblVersion('2.1')
        ->setTipoOperacion('0101')
        ->setTipoDoc(getValue($payload, 'tipoDoc', '01'))
        ->setSerie(getValue($payload, 'serie', 'F001'))
        ->setCorrelativo(getValue($payload, 'correlativo', '1'))
        ->setFechaEmision(new DateTime(getValue($payload, 'fechaEmision', date('Y-m-d H:i:sP'))))
        ->setFormaPago(new FormaPagoContado())
        ->setTipoMoneda(getValue($payload, 'moneda', 'PEN'))
        ->setCompany($company)
        ->setClient($client)
        ->setMtoOperGravadas((float)($totales['operGravadas'] ?? 0))
        ->setMtoIGV((float)($totales['igv'] ?? 0))
        ->setTotalImpuestos((float)($totales['totalImpuestos'] ?? 0))
        ->setValorVenta((float)($totales['valorVenta'] ?? 0))
        ->setSubTotal((float)($totales['subTotal'] ?? 0))
        ->setMtoImpVenta((float)($totales['totalVenta'] ?? 0));

    $details = [];
    foreach ($payload['details'] ?? [] as $det) {
        $item = (new SaleDetail())
            ->setCodProducto(getValue($det, 'codigo', 'P001'))
            ->setUnidad(getValue($det, 'unidad', 'NIU'))
            ->setCantidad((float)($det['cantidad'] ?? 1))
            ->setMtoValorUnitario((float)($det['valorUnitario'] ?? 0))
            ->setDescripcion(getValue($det, 'descripcion', 'PRODUCTO'))
            ->setMtoBaseIgv((float)($det['baseIgv'] ?? 0))
            ->setPorcentajeIgv((float)($det['porcentajeIgv'] ?? 18.00))
            ->setIgv((float)($det['igv'] ?? 0))
            ->setTipAfeIgv(getValue($det, 'tipAfeIgv', '10'))
            ->setTotalImpuestos((float)($det['totalImpuestos'] ?? 0))
            ->setMtoValorVenta((float)($det['valorVenta'] ?? 0))
            ->setMtoPrecioUnitario((float)($det['precioUnitario'] ?? 0));
        $details[] = $item;
    }

    if (empty($details)) {
        // Fallback for old single detail format
        $detalle = $payload['detalle'] ?? [];
        $item = (new SaleDetail())
            ->setCodProducto(getValue($detalle, 'codigo', 'P001'))
            ->setUnidad(getValue($detalle, 'unidad', 'NIU'))
            ->setCantidad((float)($detalle['cantidad'] ?? 1))
            ->setMtoValorUnitario((float)($detalle['valorUnitario'] ?? 0))
            ->setDescripcion(getValue($detalle, 'descripcion', 'PRODUCTO'))
            ->setMtoBaseIgv((float)($detalle['baseIgv'] ?? 0))
            ->setPorcentajeIgv((float)($detalle['porcentajeIgv'] ?? 18.00))
            ->setIgv((float)($detalle['igv'] ?? 0))
            ->setTipAfeIgv(getValue($detalle, 'tipAfeIgv', '10'))
            ->setTotalImpuestos((float)($detalle['totalImpuestos'] ?? 0))
            ->setMtoValorVenta((float)($detalle['valorVenta'] ?? 0))
            ->setMtoPrecioUnitario((float)($detalle['precioUnitario'] ?? 0));
        $details[] = $item;
    }

    $legend = (new Legend())
        ->setCode('1000')
        ->setValue(getValue($payload, 'montoLetras', 'SON ZERO CON 00/100 SOLES'));

    $invoice->setDetails($details)->setLegends([$legend]);

    return $invoice;
}

function sendInvoice(array $payload): array
{
    $ruc = getValue($payload, 'ruc', '20000000001');
    $user = getValue($payload, 'usuarioSol', 'MODDATOS');
    $password = getValue($payload, 'claveSol', 'moddatos');

    $see = createSee($ruc, $user, $password);
    $invoice = buildInvoice($payload);
    $result = $see->send($invoice);
    $xml = $see->getFactory()->getLastXml();

    if (!$result->isSuccess()) {
        $error = $result->getError();
        return [
            'success' => false,
            'message' => $error ? ($error->getCode() . ' - ' . $error->getMessage()) : 'SUNAT devolvió un error al enviar la factura.',
            'xmlBase64' => base64_encode($xml ?: ''),
            'documentName' => $invoice->getName(),
        ];
    }

    $cdr = $result->getCdrResponse();

    return [
        'success' => true,
        'message' => 'Factura enviada correctamente a SUNAT.',
        'documentName' => $invoice->getName(),
        'xmlBase64' => base64_encode($xml ?: ''),
        'cdrZipBase64' => base64_encode($result->getCdrZip() ?: ''),
        'sunatCode' => $cdr ? $cdr->getCode() : '',
        'sunatDescription' => $cdr ? $cdr->getDescription() : '',
        'sunatNotes' => $cdr ? $cdr->getNotes() : [],
    ];
}

$input = readInput();
$action = getValue($input, 'action');

try {
    switch ($action) {
        case 'validate':
            $ruc = getValue($input, 'ruc', '20000000001');
            $user = getValue($input, 'usuarioSol', 'MODDATOS');
            $password = getValue($input, 'claveSol', 'moddatos');
            respond(validateCredentials($ruc, $user, $password));
            break;

        case 'sendInvoice':
            respond(sendInvoice($input));
            break;

        default:
            respond([
                'success' => false,
                'message' => 'Acción no soportada por el puente local.',
            ], 1);
    }
} catch (Throwable $e) {
    respond([
        'success' => false,
        'message' => $e->getMessage(),
    ], 1);
}
