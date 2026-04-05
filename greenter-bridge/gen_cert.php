<?php
$res = openssl_pkey_new([
    'private_key_bits' => 2048,
    'private_key_type' => OPENSSL_KEYTYPE_RSA,
]);
openssl_pkey_export($res, $privKey);
$dn = [
    'countryName' => 'PE',
    'stateOrProvinceName' => 'Lima',
    'localityName' => 'Lima',
    'organizationName' => 'EMPRESA BETA',
    'commonName' => '20000000001',
];
$x509 = openssl_csr_new($dn, $res);
$sscert = openssl_csr_sign($x509, null, $res, 365);
openssl_x509_export($sscert, $cert);
file_put_contents('certificate.pem', $cert . $privKey);
echo "Certificado generado correctamente.\n";
