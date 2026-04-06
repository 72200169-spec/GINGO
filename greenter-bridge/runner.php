<?php

declare(strict_types=1);

// Desactivar reporte de errores a stdout para evitar corromper el JSON de salida
error_reporting(0);
ini_set('display_errors', '0');
ini_set('log_errors', '1');

use Greenter\Model\Client\Client;
use Greenter\Model\Company\Address;
use Greenter\Model\Company\Company;
use Greenter\Model\Sale\FormaPagos\FormaPagoContado;
use Greenter\Model\Sale\Invoice;
use Greenter\Model\Sale\Legend;
use Greenter\Model\Sale\SaleDetail;
use Greenter\See;
use Greenter\Ws\Services\SunatEndpoints;
use Greenter\Report\HtmlReport;
use Greenter\Report\Resolver\DefaultTemplateResolver;

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

function getValue(array $data, string $key, $default = null)
{
    return $data[$key] ?? $default;
}

try {
    $payload = readInput();
    $action = getValue($payload, 'action');

    if ($action === 'sendInvoice') {
        $see = new See();
        $certificatePath = getenv('GINGO_CERTIFICATE_PATH') ?: TEST_CERTIFICATE_PATH;
        if (!file_exists($certificatePath)) {
            respond(['success' => false, 'message' => 'Certificado no encontrado en: ' . $certificatePath], 1);
        }
        $see->setCertificate(file_get_contents($certificatePath));
        $see->setService(SunatEndpoints::FE_BETA);
        $see->setClaveSOL(getValue($payload, 'ruc', '20000000001'), getValue($payload, 'usuarioSol', 'MODDATOS'), getValue($payload, 'claveSol', 'moddatos'));

        $client = (new Client())
            ->setTipoDoc(getValue($payload['cliente'], 'tipoDoc', '6'))
            ->setNumDoc(getValue($payload['cliente'], 'numDoc', '20000000001'))
            ->setRznSocial(getValue($payload['cliente'], 'razonSocial', 'CLIENTE PRUEBA'));

        $emisorData = getValue($payload, 'emisor', []);
        $address = (new Address())
            ->setUbigueo(getValue($emisorData, 'ubigeo', '150101'))
            ->setDepartamento(getValue($emisorData, 'departamento', 'LIMA'))
            ->setProvincia(getValue($emisorData, 'provincia', 'LIMA'))
            ->setDistrito(getValue($emisorData, 'distrito', 'LIMA'))
            ->setCodLocal(getValue($emisorData, 'codLocal', '0000'))
            ->setDireccion(getValue($emisorData, 'direccion', 'AV. PRUEBA 123'));

        $company = (new Company())
            ->setRuc(getValue($emisorData, 'ruc', '20000000001'))
            ->setRazonSocial(getValue($emisorData, 'razonSocial', 'EMPRESA PRUEBA'))
            ->setNombreComercial(getValue($emisorData, 'nombreComercial', 'EMPRESA PRUEBA'))
            ->setAddress($address);

        $details = [];
        $calculatedMtoOperGravadas = 0;
        $calculatedMtoIGV = 0;
        $calculatedTotalImpuestos = 0;
        $calculatedTotalVenta = 0;

        foreach ($payload['details'] ?? [] as $det) {
            $cantidad = (float)($det['cantidad'] ?? 1);
            $valorUnitario = (float)($det['valorUnitario'] ?? 0);
            $precioUnitario = (float)($det['precioUnitario'] ?? 0);
            $baseIgv = (float)($det['baseIgv'] ?? 0);
            $igv = (float)($det['igv'] ?? 0);
            $tipAfeIgv = getValue($det, 'tipAfeIgv', '10');

            $item = (new SaleDetail())
                ->setCodProducto(getValue($det, 'codigo', 'P001'))
                ->setUnidad(getValue($det, 'unidad', 'NIU'))
                ->setCantidad($cantidad)
                ->setMtoValorUnitario($valorUnitario)
                ->setDescripcion(getValue($det, 'descripcion', 'PRODUCTO'))
                ->setMtoBaseIgv($baseIgv)
                ->setPorcentajeIgv(18.00)
                ->setIgv($igv)
                ->setTipAfeIgv($tipAfeIgv)
                ->setTotalImpuestos($igv)
                ->setMtoValorVenta($baseIgv)
                ->setMtoPrecioUnitario($precioUnitario);
            
            $details[] = $item;

            if ($tipAfeIgv === '10') {
                $calculatedMtoOperGravadas += $baseIgv;
                $calculatedMtoIGV += $igv;
                $calculatedTotalImpuestos += $igv;
            }
            $calculatedTotalVenta += ($baseIgv + $igv);
        }

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
            ->setMtoOperGravadas(round($calculatedMtoOperGravadas, 2))
            ->setMtoIGV(round($calculatedMtoIGV, 2))
            ->setTotalImpuestos(round($calculatedTotalImpuestos, 2))
            ->setValorVenta(round($calculatedMtoOperGravadas, 2))
            ->setSubTotal(round($calculatedTotalVenta, 2))
            ->setMtoImpVenta(round($calculatedTotalVenta, 2));

        $legend = (new Legend())
            ->setCode('1000')
            ->setValue(getValue($payload, 'montoLetras', 'SON ' . number_format($calculatedTotalVenta, 2, '.', '') . ' SOLES'));

        $invoice->setDetails($details)->setLegends([$legend]);

        $result = $see->send($invoice);

        // Directorio de almacenamiento
        $storageDir = __DIR__ . '/archivos_sunat';
        if (!is_dir($storageDir)) {
            mkdir($storageDir, 0777, true);
        }

        $fileName = $invoice->getName();
        $xmlPath = $storageDir . '/' . $fileName . '.xml';
        $cdrPath = '';
        $pdfPath = '';

        // Guardar XML
        file_put_contents($xmlPath, $see->getFactory()->getLastXml());
        
        if ($result->isSuccess()) {
            // Guardar CDR
            $cdrPath = $storageDir . '/R-' . $fileName . '.zip';
            file_put_contents($cdrPath, $result->getCdrZip());

            // Generar PDF
            try {
                $report = new HtmlReport();
                $resolver = new DefaultTemplateResolver();
                $report->setTemplate($resolver->getTemplate($invoice));
                
                $params = [
                    'system' => [
                        'logo' => '',
                        'hash' => '',
                    ],
                    'user' => [
                        'header' => 'EMPRESA PRUEBA',
                        'footer' => 'Gracias por su compra',
                    ]
                ];

                $html = $report->render($invoice, $params);
                
                $dompdf = new \Dompdf\Dompdf([
                    'isRemoteEnabled' => true,
                    'defaultFont' => 'Arial',
                ]);
                $dompdf->loadHtml($html);
                $dompdf->setPaper('A4', 'portrait');
                $dompdf->render();
                $pdfContent = $dompdf->output();
                
                $pdfPath = $storageDir . '/' . $fileName . '.pdf';
                file_put_contents($pdfPath, $pdfContent);
            } catch (Throwable $reportError) {
                // Si falla el PDF, continuamos pero guardamos el error en el log
                error_log("Error generando PDF: " . $reportError->getMessage());
            }

            // Registrar en base de datos (MySQL)
            try {
                $dbHost = getenv('GINGO_DB_HOST') ?: '127.0.0.1';
                $dbPort = getenv('GINGO_DB_PORT') ?: '13306';
                $dbName = getenv('GINGO_DB_NAME') ?: 'gingo';
                $dbUser = getenv('GINGO_DB_USER') ?: 'gingo_app';
                $dbPassword = getenv('GINGO_DB_PASSWORD') ?: 'gingo_password';

                $pdo = new PDO("mysql:host={$dbHost};port={$dbPort};dbname={$dbName}", $dbUser, $dbPassword);
                $stmt = $pdo->prepare("INSERT INTO comprobantes (tipo_doc, serie, correlativo, fecha_emision, ruc_emisor, doc_cliente, nombre_cliente, total, xml_path, cdr_path, pdf_path, estado_sunat) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
                $stmt->execute([
                    $invoice->getTipoDoc(),
                    $invoice->getSerie(),
                    $invoice->getCorrelativo(),
                    $invoice->getFechaEmision()->format('Y-m-d H:i:s'),
                    $company->getRuc(),
                    $client->getNumDoc(),
                    $client->getRznSocial(),
                    $invoice->getMtoImpVenta(),
                    $xmlPath,
                    $cdrPath,
                    $pdfPath,
                    'ACEPTADO'
                ]);
            } catch (PDOException $dbError) {
                error_log("Error en DB: " . $dbError->getMessage());
            }
        }

        if (!$result->isSuccess()) {
            respond([
                'success' => false,
                'message' => 'Error al enviar a SUNAT: ' . $result->getError()->getMessage(),
                'code' => $result->getError()->getCode(),
            ]);
        }

        respond([
            'success' => true,
            'message' => 'Factura enviada correctamente a SUNAT.',
            'documentName' => $fileName,
            'xmlPath' => $xmlPath,
            'cdrPath' => $cdrPath,
            'pdfPath' => $pdfPath,
            'sunatCode' => $result->getCdrResponse()->getCode(),
            'sunatDescription' => $result->getCdrResponse()->getDescription(),
        ]);
    } else {
        respond([
            'success' => false,
            'message' => 'Acción no soportada: ' . $action,
        ]);
    }
} catch (Throwable $e) {
    respond([
        'success' => false,
        'message' => 'Error crítico en el bridge: ' . $e->getMessage(),
        'trace' => $e->getTraceAsString()
    ], 1);
}
