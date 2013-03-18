function Get-FileName($line)
{
    $index = $_.IndexOf("log:"); 
    return $_.Substring(0, $index) + "log" 
}

# UnobservedTask exceptions
findstr /snip unobserved *.log | %{ Get-FileName $_ } | unique > unobserved_exceptions.txt

# ObjectDisposed exceptions
findstr /snip disposed *.log | %{ Get-FileName $_ } | unique > ode_exceptions.txt

# Network errors exceptions
findstr /snip unexpected error *.log | %{ Get-FileName $_ } | unique > unexpected.txt
