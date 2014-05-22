using System;
using System.Threading;

namespace Lucene.Net.Util
{

	/*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements.  See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License.  You may obtain a copy of the License at
	 *
	 *     http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */

	using Directory = Lucene.Net.Store.Directory;


	/// <summary>
	/// this class emulates the new Java 7 "Try-With-Resources" statement.
	/// Remove once Lucene is on Java 7.
	/// @lucene.internal 
	/// </summary>
	public sealed class IOUtils
	{

	  /// <summary>
	  /// UTF-8 <seealso cref="Charset"/> instance to prevent repeated
	  /// <seealso cref="Charset#forName(String)"/> lookups </summary>
	  /// @deprecated Use <seealso cref="StandardCharsets#UTF_8"/> instead. 
	  [Obsolete("Use <seealso cref="StandardCharsets#UTF_8"/> instead.")]
	  public static readonly Charset CHARSET_UTF_8 = StandardCharsets.UTF_8;

	  /// <summary>
	  /// UTF-8 charset string.
	  /// <p>Where possible, use <seealso cref="StandardCharsets#UTF_8"/> instead,
	  /// as using the String constant may slow things down. </summary>
	  /// <seealso cref= StandardCharsets#UTF_8 </seealso>
	  public static readonly string UTF_8 = StandardCharsets.UTF_8.name();

	  private IOUtils() // no instance
	  {
	  }

	  /// <summary>
	  /// <p>Closes all given <tt>IDisposable</tt>s, suppressing all thrown exceptions. Some of the <tt>IDisposable</tt>s
	  /// may be null, they are ignored. After everything is closed, method either throws <tt>priorException</tt>,
	  /// if one is supplied, or the first of suppressed exceptions, or completes normally.</p>
	  /// <p>Sample usage:<br/>
	  /// <pre class="prettyprint">
	  /// IDisposable resource1 = null, resource2 = null, resource3 = null;
	  /// ExpectedException priorE = null;
	  /// try {
	  ///   resource1 = ...; resource2 = ...; resource3 = ...; // Acquisition may throw ExpectedException
	  ///   ..do..stuff.. // May throw ExpectedException
	  /// } catch (ExpectedException e) {
	  ///   priorE = e;
	  /// } finally {
	  ///   closeWhileHandlingException(priorE, resource1, resource2, resource3);
	  /// }
	  /// </pre>
	  /// </p> </summary>
	  /// <param name="priorException">  <tt>null</tt> or an exception that will be rethrown after method completion </param>
	  /// <param name="objects">         objects to call <tt>close()</tt> on </param>
	  public static void closeWhileHandlingException<E>(E priorException, params IDisposable[] objects) where E : Exception
	  {
		Exception th = null;

		foreach (IDisposable @object in objects)
		{
		  try
		  {
			if (@object != null)
			{
			  @object.close();
			}
		  }
		  catch (Exception t)
		  {
			AddSuppressed((priorException == null) ? th : priorException, t);
			if (th == null)
			{
			  th = t;
			}
		  }
		}

		if (priorException != null)
		{
		  throw priorException;
		}
		else
		{
		  ReThrow(th);
		}
	  }

	  /// <summary>
	  /// Closes all given <tt>IDisposable</tt>s, suppressing all thrown exceptions. </summary>
	  /// <seealso cref= #closeWhileHandlingException(Exception, IDisposable...)  </seealso>
	  public static void closeWhileHandlingException<E, T1>(E priorException, IEnumerable<T1> objects) where E : Exception where T1 : java.io.IDisposable
	  {
		Exception th = null;

		foreach (IDisposable @object in objects)
		{
		  try
		  {
			if (@object != null)
			{
			  @object.close();
			}
		  }
		  catch (Exception t)
		  {
			AddSuppressed((priorException == null) ? th : priorException, t);
			if (th == null)
			{
			  th = t;
			}
		  }
		}

		if (priorException != null)
		{
		  throw priorException;
		}
		else
		{
		  ReThrow(th);
		}
	  }

	  /// <summary>
	  /// Closes all given <tt>IDisposable</tt>s.  Some of the
	  /// <tt>IDisposable</tt>s may be null; they are
	  /// ignored.  After everything is closed, the method either
	  /// throws the first exception it hit while closing, or
	  /// completes normally if there were no exceptions.
	  /// </summary>
	  /// <param name="objects">
	  ///          objects to call <tt>close()</tt> on </param>
	  public static void Close(params IDisposable[] objects)
	  {
		Exception th = null;

		foreach (IDisposable @object in objects)
		{
		  try
		  {
			if (@object != null)
			{
			  @object.close();
			}
		  }
		  catch (Exception t)
		  {
			AddSuppressed(th, t);
			if (th == null)
			{
			  th = t;
			}
		  }
		}

		ReThrow(th);
	  }

	  /// <summary>
	  /// Closes all given <tt>IDisposable</tt>s. </summary>
	  /// <seealso cref= #close(IDisposable...) </seealso>
	  public static void close<T1>(IEnumerable<T1> objects) where T1 : java.io.IDisposable
	  {
		Exception th = null;

		foreach (IDisposable @object in objects)
		{
		  try
		  {
			if (@object != null)
			{
			  @object.close();
			}
		  }
		  catch (Exception t)
		  {
			AddSuppressed(th, t);
			if (th == null)
			{
			  th = t;
			}
		  }
		}

		ReThrow(th);
	  }

	  /// <summary>
	  /// Closes all given <tt>IDisposable</tt>s, suppressing all thrown exceptions.
	  /// Some of the <tt>IDisposable</tt>s may be null, they are ignored.
	  /// </summary>
	  /// <param name="objects">
	  ///          objects to call <tt>close()</tt> on </param>
	  public static void CloseWhileHandlingException(params IDisposable[] objects)
	  {
		foreach (IDisposable @object in objects)
		{
		  try
		  {
			if (@object != null)
			{
			  @object.close();
			}
		  }
		  catch (Exception t)
		  {
		  }
		}
	  }

	  /// <summary>
	  /// Closes all given <tt>IDisposable</tt>s, suppressing all thrown exceptions. </summary>
	  /// <seealso cref= #closeWhileHandlingException(IDisposable...) </seealso>
	  public static void closeWhileHandlingException<T1>(IEnumerable<T1> objects) where T1 : java.io.IDisposable
	  {
		foreach (IDisposable @object in objects)
		{
		  try
		  {
			if (@object != null)
			{
			  @object.close();
			}
		  }
		  catch (Exception t)
		  {
		  }
		}
	  }

	  /// <summary>
	  /// adds a Throwable to the list of suppressed Exceptions of the first Throwable </summary>
	  /// <param name="exception"> this exception should get the suppressed one added </param>
	  /// <param name="suppressed"> the suppressed exception </param>
	  private static void AddSuppressed(Exception exception, Exception suppressed)
	  {
		if (exception != null && suppressed != null)
		{
		  exception.addSuppressed(suppressed);
		}
	  }

	  /// <summary>
	  /// Wrapping the given <seealso cref="InputStream"/> in a reader using a <seealso cref="CharsetDecoder"/>.
	  /// Unlike Java's defaults this reader will throw an exception if your it detects 
	  /// the read charset doesn't match the expected <seealso cref="Charset"/>. 
	  /// <p>
	  /// Decoding readers are useful to load configuration files, stopword lists or synonym files
	  /// to detect character set problems. However, its not recommended to use as a common purpose 
	  /// reader.
	  /// </summary>
	  /// <param name="stream"> the stream to wrap in a reader </param>
	  /// <param name="charSet"> the expected charset </param>
	  /// <returns> a wrapping reader </returns>
	  public static Reader GetDecodingReader(InputStream stream, Charset charSet)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.nio.charset.CharsetDecoder charSetDecoder = charSet.newDecoder().onMalformedInput(java.nio.charset.CodingErrorAction.REPORT).onUnmappableCharacter(java.nio.charset.CodingErrorAction.REPORT);
		CharsetDecoder charSetDecoder = charSet.newDecoder().onMalformedInput(CodingErrorAction.REPORT).onUnmappableCharacter(CodingErrorAction.REPORT);
		return new BufferedReader(new InputStreamReader(stream, charSetDecoder));
	  }

	  /// <summary>
	  /// Opens a Reader for the given <seealso cref="File"/> using a <seealso cref="CharsetDecoder"/>.
	  /// Unlike Java's defaults this reader will throw an exception if your it detects 
	  /// the read charset doesn't match the expected <seealso cref="Charset"/>. 
	  /// <p>
	  /// Decoding readers are useful to load configuration files, stopword lists or synonym files
	  /// to detect character set problems. However, its not recommended to use as a common purpose 
	  /// reader. </summary>
	  /// <param name="file"> the file to open a reader on </param>
	  /// <param name="charSet"> the expected charset </param>
	  /// <returns> a reader to read the given file </returns>
	  public static Reader GetDecodingReader(File file, Charset charSet)
	  {
		FileInputStream stream = null;
		bool success = false;
		try
		{
		  stream = new FileInputStream(file);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.io.Reader reader = getDecodingReader(stream, charSet);
		  Reader reader = GetDecodingReader(stream, charSet);
		  success = true;
		  return reader;

		}
		finally
		{
		  if (!success)
		  {
			IOUtils.Close(stream);
		  }
		}
	  }

	  /// <summary>
	  /// Opens a Reader for the given resource using a <seealso cref="CharsetDecoder"/>.
	  /// Unlike Java's defaults this reader will throw an exception if your it detects 
	  /// the read charset doesn't match the expected <seealso cref="Charset"/>. 
	  /// <p>
	  /// Decoding readers are useful to load configuration files, stopword lists or synonym files
	  /// to detect character set problems. However, its not recommended to use as a common purpose 
	  /// reader. </summary>
	  /// <param name="clazz"> the class used to locate the resource </param>
	  /// <param name="resource"> the resource name to load </param>
	  /// <param name="charSet"> the expected charset </param>
	  /// <returns> a reader to read the given file
	  ///  </returns>
	  public static Reader GetDecodingReader(Type clazz, string resource, Charset charSet)
	  {
		InputStream stream = null;
		bool success = false;
		try
		{
		  stream = clazz.getResourceAsStream(resource);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.io.Reader reader = getDecodingReader(stream, charSet);
		  Reader reader = GetDecodingReader(stream, charSet);
		  success = true;
		  return reader;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.Close(stream);
		  }
		}
	  }

	  /// <summary>
	  /// Deletes all given files, suppressing all thrown IOExceptions.
	  /// <p>
	  /// Note that the files should not be null.
	  /// </summary>
	  public static void DeleteFilesIgnoringExceptions(Directory dir, params string[] files)
	  {
		foreach (string name in files)
		{
		  try
		  {
			dir.DeleteFile(name);
		  }
		  catch (Exception ignored)
		  {
			// ignore
		  }
		}
	  }

	  /// <summary>
	  /// Copy one file's contents to another file. The target will be overwritten
	  /// if it exists. The source must exist.
	  /// </summary>
	  public static void Copy(File source, File target)
	  {
		FileInputStream fis = null;
		FileOutputStream fos = null;
		try
		{
		  fis = new FileInputStream(source);
		  fos = new FileOutputStream(target);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte [] buffer = new byte [1024 * 8];
		  sbyte[] buffer = new sbyte [1024 * 8];
		  int len;
		  while ((len = fis.read(buffer)) > 0)
		  {
			fos.write(buffer, 0, len);
		  }
		}
		finally
		{
		  close(fis, fos);
		}
	  }

	  /// <summary>
	  /// Simple utilty method that takes a previously caught
	  /// {@code Throwable} and rethrows either {@code
	  /// IOException} or an unchecked exception.  If the
	  /// argument is null then this method does nothing.
	  /// </summary>
	  public static void ReThrow(Exception th)
	  {
		if (th != null)
		{
		  if (th is IOException)
		  {
			throw (IOException) th;
		  }
		  ReThrowUnchecked(th);
		}
	  }

	  /// <summary>
	  /// Simple utilty method that takes a previously caught
	  /// {@code Throwable} and rethrows it as an unchecked exception.
	  /// If the argument is null then this method does nothing.
	  /// </summary>
	  public static void ReThrowUnchecked(Exception th)
	  {
		if (th != null)
		{
		  if (th is Exception)
		  {
			throw (Exception) th;
		  }
		  if (th is Exception)
		  {
			throw (Exception) th;
		  }
		  throw new Exception(th);
		}
	  }

	  /// <summary>
	  /// Ensure that any writes to the given file is written to the storage device that contains it. </summary>
	  /// <param name="fileToSync"> the file to fsync </param>
	  /// <param name="isDir"> if true, the given file is a directory (we open for read and ignore IOExceptions,
	  ///  because not all file systems and operating systems allow to fsync on a directory) </param>
	  public static void Fsync(File fileToSync, bool isDir)
	  {
		IOException exc = null;

		// If the file is a directory we have to open read-only, for regular files we must open r/w for the fsync to have an effect.
		// See http://blog.httrack.com/blog/2013/11/15/everything-you-always-wanted-to-know-about-fsync/
		using (final FileChannel file = FileChannel.open(fileToSync.toPath(), isDir ? StandardOpenOption.READ : StandardOpenOption.WRITE))
		{
				try
				{
			  for (int retry = 0; retry < 5; retry++)
			  {
				try
				{
				  file.force(true);
				  return;
				}
				catch (IOException ioe)
				{
				  if (exc == null)
				  {
					exc = ioe;
				  }
				  try
				  {
					// Pause 5 msec
					Thread.Sleep(5L);
				  }
				  catch (InterruptedException ie)
				  {
					ThreadInterruptedException ex = new ThreadInterruptedException(ie);
					ex.addSuppressed(exc);
					throw ex;
				  }
				}
			  }
				}
			catch (IOException ioe)
			{
			  if (exc == null)
			  {
				exc = ioe;
			  }
			}
		}

		if (isDir)
		{
		  assert(Constants.LINUX || Constants.MAC_OS_X) == false : "On Linux and MacOSX fsyncing a directory should not throw IOException, " + "we just don't want to rely on that in production (undocumented). Got: " + exc;
		  // Ignore exception if it is a directory
		  return;
		}

		// Throw original exception
		throw exc;
	  }
	}

}