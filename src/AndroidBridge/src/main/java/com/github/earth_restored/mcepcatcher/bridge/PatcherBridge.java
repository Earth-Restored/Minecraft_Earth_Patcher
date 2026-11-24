package com.github.earth_restored.mcepcatcher.bridge;

import com.android.apksig.ApkSigner;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.PrintWriter;
import java.io.StringWriter;
import java.security.KeyStore;
import java.security.PrivateKey;
import java.security.cert.Certificate;
import java.security.cert.X509Certificate;
import java.text.MessageFormat;
import java.util.Collections;
import java.util.logging.Handler;
import java.util.logging.Level;
import java.util.logging.LogRecord;
import java.util.logging.Logger;

import brut.androlib.ApkBuilder;
import brut.androlib.ApkDecoder;
import brut.androlib.Config;

public class PatcherBridge {

    static {
        ensureSystemProperties(null);
    }

    private static void ensureSystemProperties(String frameworkDirPath) {
        if (Thread.currentThread().getContextClassLoader() == null) {
            Thread.currentThread().setContextClassLoader(PatcherBridge.class.getClassLoader());
        }

        try {
            javax.imageio.spi.IIORegistry.getDefaultInstance();
        } catch (Throwable ignored) {
        }

        if (System.getProperty("sun.arch.data.model") == null) {
            System.setProperty("sun.arch.data.model", "64");
        }

        if (System.getProperty("os.name") == null) {
            System.setProperty("os.name", "Linux");
        }

        if (System.getProperty("user.home") == null) {
            if (frameworkDirPath != null && !frameworkDirPath.isEmpty()) {
                File parent = new File(frameworkDirPath).getParentFile();
                if (parent != null) {
                    System.setProperty("user.home", parent.getAbsolutePath());
                } else {
                    System.setProperty("user.home", frameworkDirPath);
                }
            } else {
                System.setProperty("user.home", "/data/local/tmp");
            }
        }
    }

    public static void ensureFramework(String frameworkDirPath, LogCallback callback) {
        if (frameworkDirPath == null || frameworkDirPath.isEmpty()) {
            return;
        }

        try {
            File frameworkDir = new File(frameworkDirPath);
            if (!frameworkDir.exists()) {
                frameworkDir.mkdirs();
            }

            File target1Apk = new File(frameworkDir, "1.apk");
            if (target1Apk.exists() && target1Apk.length() > 0) {
                return;
            }

            if (callback != null) {
                callback.onLog("Preparing Android framework resources (1.apk)...");
            }

            String[] candidates = new String[] {
                    "/prebuilt/android-framework.dat",
                    "/framework/1.apk",
                    "/prebuilt/android-framework.jar",
                    "prebuilt/android-framework.dat",
                    "framework/1.apk",
                    "prebuilt/android-framework.jar"
            };

            InputStream apkInputStream = null;
            ClassLoader cl = PatcherBridge.class.getClassLoader();
            for (String path : candidates) {
                if (path.startsWith("/")) {
                    apkInputStream = PatcherBridge.class.getResourceAsStream(path);
                } else if (cl != null) {
                    apkInputStream = cl.getResourceAsStream(path);
                }
                if (apkInputStream != null) {
                    break;
                }
            }

            if (apkInputStream != null) {
                try (InputStream in = apkInputStream;
                        FileOutputStream apkOutputStream = new FileOutputStream(target1Apk)) {
                    byte[] buf = new byte[8192];
                    int len;
                    while ((len = in.read(buf)) != -1) {
                        apkOutputStream.write(buf, 0, len);
                    }
                }
                if (callback != null) {
                    callback.onLog("Installed Android framework 1.apk (" + target1Apk.length() + " bytes) at: "
                            + target1Apk.getAbsolutePath());
                }
            } else {
                if (callback != null) {
                    callback.onLog(
                            "Warning: Could not locate embedded framework stream (1.apk). Apktool will attempt internal resolution.");
                }
            }
        } catch (Throwable t) {
            logError(callback, "Warning during framework extraction", t);
        }
    }

    public static boolean decompile(String apkPath, String outDirPath, String frameworkDirPath, LogCallback callback) {
        ensureSystemProperties(frameworkDirPath);
        ensureFramework(frameworkDirPath, callback);
        Handler logHandler = attachLogHandler(callback);
        try {
            if (callback != null) {
                callback.onLog("Initializing Apktool decoder...");
            }

            File apkFile = new File(apkPath);
            File outDir = new File(outDirPath);

            Config config = new Config("3.0.3");
            config.setForced(true);
            if (frameworkDirPath != null && !frameworkDirPath.isEmpty()) {
                File fwDir = new File(frameworkDirPath);
                if (!fwDir.exists()) {
                    fwDir.mkdirs();
                }

                config.setFrameworkDirectory(fwDir.getAbsolutePath());
            }

            if (callback != null) {
                callback.onLog("Decompiling APK: " + apkFile.getName() + " -> " + outDir.getName());
            }

            ApkDecoder decoder = new ApkDecoder(apkFile, config);
            decoder.decode(outDir);

            if (callback != null) {
                callback.onLog("Decompilation completed successfully.");
            }

            return true;
        } catch (Throwable t) {
            logError(callback, "Error during decompilation", t);
            return false;
        } finally {
            detachLogHandler(logHandler);
        }
    }

    public static boolean recompile(String inDirPath, String outApkPath, String aaptPath, String frameworkDirPath,
            LogCallback callback) {
        ensureSystemProperties(frameworkDirPath);
        ensureFramework(frameworkDirPath, callback);
        Handler logHandler = attachLogHandler(callback);

        try {
            if (callback != null) {
                callback.onLog("Initializing Apktool builder...");
            }

            File inDir = new File(inDirPath);
            File outApk = new File(outApkPath);

            if (outApk.exists()) {
                outApk.delete();
            }

            Config config = new Config("3.0.3");
            config.setForced(true);
            if (frameworkDirPath != null && !frameworkDirPath.isEmpty()) {
                File fwDir = new File(frameworkDirPath);
                if (!fwDir.exists()) {
                    fwDir.mkdirs();
                }

                config.setFrameworkDirectory(fwDir.getAbsolutePath());
            }

            if (aaptPath != null && !aaptPath.isEmpty()) {
                config.setAaptBinary(aaptPath);
            }

            if (callback != null) {
                callback.onLog("Recompiling directory: " + inDir.getName() + " -> " + outApk.getName());
            }

            ApkBuilder builder = new ApkBuilder(inDir, config);
            builder.build(outApk);

            if (callback != null) {
                callback.onLog("Recompilation completed successfully.");
            }

            return outApk.exists();
        } catch (Throwable t) {
            logError(callback, "Error during recompilation", t);
            return false;
        } finally {
            detachLogHandler(logHandler);
        }
    }

    public static boolean sign(String inApkPath, String outApkPath, String keystorePath, String keystorePass,
            String keyAlias, LogCallback callback) {
        try {
            if (callback != null) {
                callback.onLog("Signing APK using Google apksig...");
            }

            File inApk = new File(inApkPath);
            File outApk = new File(outApkPath);

            if (!inApk.exists()) {
                if (callback != null) {
                    callback.onLog("Input APK to sign does not exist: " + inApkPath);
                }

                return false;
            }

            if (outApk.exists()) {
                outApk.delete();
            }

            char[] pass = (keystorePass != null && !keystorePass.isEmpty()) ? keystorePass.toCharArray()
                    : "mcepcatcher".toCharArray();
            String alias = (keyAlias != null && !keyAlias.isEmpty()) ? keyAlias : "mcep";

            KeyStore ks = loadKeyStore(keystorePath, pass, alias);
            PrivateKey privateKey = (PrivateKey) ks.getKey(alias, pass);
            Certificate cert = ks.getCertificate(alias);

            if (privateKey == null || cert == null) {
                if (callback != null) {
                    callback.onLog("Failed to load key/certificate for signing.");
                }

                return false;
            }

            ApkSigner.SignerConfig signerConfig = new ApkSigner.SignerConfig.Builder(
                    alias,
                    privateKey,
                    Collections.singletonList((X509Certificate) cert)).build();

            ApkSigner.Builder signerBuilder = new ApkSigner.Builder(Collections.singletonList(signerConfig))
                    .setInputApk(inApk)
                    .setOutputApk(outApk)
                    .setV1SigningEnabled(true)
                    .setV2SigningEnabled(true)
                    .setOtherSignersSignaturesPreserved(false);

            signerBuilder.build().sign();

            if (callback != null) {
                callback.onLog("Signing completed successfully: " + outApk.getName());
            }

            return outApk.exists();
        } catch (Throwable t) {
            logError(callback, "Error during APK signing", t);
            return false;
        }
    }

    private static void logError(LogCallback callback, String prefix, Throwable t) {
        StringWriter sw = new StringWriter();
        PrintWriter pw = new PrintWriter(sw);
        t.printStackTrace(pw);
        String trace = sw.toString();

        StringBuilder sb = new StringBuilder();
        sb.append(prefix).append(": ").append(t.getClass().getName());
        if (t.getMessage() != null && !t.getMessage().isEmpty()) {
            sb.append(": ").append(t.getMessage());
        }

        Throwable cause = t.getCause();
        while (cause != null) {
            sb.append("\n  Caused by: ").append(cause.getClass().getName());
            if (cause.getMessage() != null && !cause.getMessage().isEmpty()) {
                sb.append(": ").append(cause.getMessage());
            }

            cause = cause.getCause();
        }

        sb.append("\n").append(trace);

        String fullMessage = sb.toString();
        if (callback != null) {
            callback.onLog(fullMessage);
        }

        System.err.println(fullMessage);
    }

    private static KeyStore loadKeyStore(String keystorePath, char[] pass, String alias) throws Exception {
        KeyStore ks = KeyStore.getInstance("PKCS12");
        if (keystorePath != null && !keystorePath.isEmpty() && new File(keystorePath).exists()) {
            try (FileInputStream fis = new FileInputStream(keystorePath)) {
                ks.load(fis, pass);
                return ks;
            }
        }

        try (InputStream is = PatcherBridge.class.getResourceAsStream("/mcep_debug.p12")) {
            if (is != null) {
                ks.load(is, "mcepcatcher".toCharArray());
                return ks;
            }
        }

        throw new IllegalStateException("Embedded signing key /mcep_debug.p12 could not be loaded.");
    }

    private static Handler attachLogHandler(final LogCallback callback) {
        if (callback == null)
            return null;
        Handler handler = new Handler() {
            @Override
            public void publish(LogRecord record) {
                if (record != null && record.getMessage() != null) {
                    String msg = record.getMessage();
                    Object[] params = record.getParameters();
                    if (params != null && params.length > 0) {
                        try {
                            msg = MessageFormat.format(msg, params);
                        } catch (Exception ignored) {
                        }
                    }

                    if (record.getThrown() != null) {
                        StringWriter sw = new StringWriter();
                        record.getThrown().printStackTrace(new PrintWriter(sw));
                        msg += "\n" + sw.toString();
                    }

                    callback.onLog(msg);
                }
            }

            @Override
            public void flush() {
            }

            @Override
            public void close() throws SecurityException {
            }
        };

        handler.setLevel(Level.ALL);
        Logger brutLogger = Logger.getLogger("brut");
        brutLogger.setLevel(Level.ALL);
        brutLogger.setUseParentHandlers(false);
        brutLogger.addHandler(handler);
        return handler;
    }

    private static void detachLogHandler(Handler handler) {
        if (handler != null) {
            try {
                Logger brutLogger = Logger.getLogger("brut");
                brutLogger.removeHandler(handler);
            } catch (Exception ignored) {
            }
        }
    }
}
